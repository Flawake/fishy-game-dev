#!/usr/bin/env python3
"""Generate Unity-friendly C# DTOs from an OpenAPI 3.x document.

Output is tailored to Unity's JsonUtility, which is what
Assets/Database communication/DatabaseCommunications.cs uses:

  * public *fields*, not properties -- JsonUtility ignores properties entirely.
  * snake_case names preserved, so no serializer configuration is needed and
    the wire format matches the backend exactly.
  * uuid / date-time surfaced as string -- JsonUtility supports neither Guid
    nor DateTime.
  * schema enums surfaced as string plus a constants class -- JsonUtility
    writes C# enums as integers, which the backend does not accept.

This is deliberately not NSwag/openapi-generator: both emit properties,
Guid/DateTimeOffset and an AdditionalProperties dictionary, none of which
survive JsonUtility.
"""

import argparse
import collections
import json
import os
import re
import sys

VALUE_TYPES = {"int", "long", "float", "double", "bool"}


def die(msg):
    print(f"error: {msg}", file=sys.stderr)
    sys.exit(1)


def ref_name(ref):
    return ref.rsplit("/", 1)[-1]


def is_enum(schema):
    return "enum" in schema and schema.get("type") == "string"


def type_names(schema):
    """OpenAPI 3.1 allows `"type": ["string", "null"]`."""
    t = schema.get("type")
    if t is None:
        return []
    return t if isinstance(t, list) else [t]


def unwrap_nullable(schema):
    """Collapse the wrappers used to express an optional value.

    utoipa renders a Rust `Option<T>` as `oneOf: [{type: null}, {$ref: T}]`,
    and OpenAPI 3.0 tooling often wraps a lone `$ref` in `allOf`. Both mean
    "T, possibly absent", so reduce them to the inner schema.
    """
    for key in ("oneOf", "anyOf", "allOf"):
        branches = schema.get(key)
        if not branches:
            continue
        concrete = [b for b in branches if type_names(b) != ["null"]]
        nullable = len(concrete) != len(branches)
        if len(concrete) == 1:
            inner, inner_nullable = unwrap_nullable(concrete[0])
            return inner, (nullable or inner_nullable)
    return schema, False


def base_type(schema, schemas):
    """Return (csharp_type, default_for_required, nullable_from_schema)."""
    schema, wrapped_nullable = unwrap_nullable(schema)
    if wrapped_nullable:
        inner, default, _ = base_type(schema, schemas)
        return inner, default, True

    if "$ref" in schema:
        name = ref_name(schema["$ref"])
        target = schemas.get(name)
        if target is None:
            die(f"unresolved $ref: {schema['$ref']}")
        if is_enum(target):
            # Serialized as its string value; see module docstring.
            return "string", "string.Empty", False
        return name, f"new {name}()", False

    types = type_names(schema)
    nullable = "null" in types
    concrete = [t for t in types if t != "null"]

    if not concrete:
        return "string", "string.Empty", True

    t = concrete[0]
    fmt = schema.get("format")

    if t == "string":
        return "string", "string.Empty", nullable
    if t == "boolean":
        return "bool", "false", nullable
    if t == "integer":
        return ("long", "0", nullable) if fmt == "int64" else ("int", "0", nullable)
    if t == "number":
        return ("double", "0", nullable) if fmt == "double" else ("float", "0f", nullable)
    if t == "array":
        items = schema.get("items")
        if not items:
            die("array schema without `items`")
        inner, _, _ = base_type(items, schemas)
        return f"List<{inner}>", f"new List<{inner}>()", nullable
    if t == "object":
        # Free-form object; JsonUtility cannot represent it, keep the raw JSON.
        return "string", "string.Empty", nullable

    die(f"unsupported schema type: {t!r}")


def field_decl(name, schema, required, schemas):
    cs_type, default, schema_nullable = base_type(schema, schemas)
    optional = schema_nullable or not required

    # Nullable value types are skipped by JsonUtility, so optional value types
    # keep their non-nullable form and a zero default -- matching the style in
    # DatabaseRequestStructs.cs.
    if optional and cs_type not in VALUE_TYPES:
        return f"        public {cs_type}? {name} = null;"
    return f"        public {cs_type} {name} = {default};"


def doc_comment(schema, indent):
    desc = schema.get("description")
    if not desc:
        return []
    out = [f"{indent}/// <summary>"]
    for line in desc.split("\n"):
        out.append(f"{indent}/// {line.strip()}")
    out.append(f"{indent}/// </summary>")
    return out


def render_enum(name, schema):
    lines = doc_comment(schema, "    ")
    lines.append(f"    public static class {name}")
    lines.append("    {")
    for value in schema["enum"]:
        ident = "".join(c if c.isalnum() else "_" for c in str(value))
        if ident and ident[0].isdigit():
            ident = "_" + ident
        lines.append(f'        public const string {ident} = "{value}";')
    lines.append("    }")
    return lines


def render_class(name, schema, schemas):
    required = set(schema.get("required", []))
    lines = doc_comment(schema, "    ")
    lines.append("    [Serializable]")
    # partial so hand-written companion files can add helper properties
    # (Guid parsing, DateTime parsing, ...) without touching generated code.
    lines.append(f"    public partial class {name}")
    lines.append("    {")
    props = schema.get("properties", {})
    if not props:
        lines.append("        // no properties defined in the schema")
    for prop, prop_schema in props.items():
        lines.extend(doc_comment(prop_schema, "        "))
        lines.append(field_decl(prop, prop_schema, prop in required, schemas))
    lines.append("    }")
    return lines


def pascal(name):
    """add_or_update_item / addOrUpdateItem / add-effect -> AddOrUpdateItem."""
    parts = [p for p in re.split(r"[^A-Za-z0-9]+", name) if p]
    out = []
    for p in parts:
        # Preserve interior capitals of an already-camelCased id.
        out.append(p[0].upper() + p[1:])
    return "".join(out) or "Unnamed"


def success_response(op):
    """The 2xx response a caller cares about, or None."""
    for code, resp in sorted((op.get("responses") or {}).items()):
        if code.startswith("2"):
            return resp
    return None


def response_binding(op, schemas):
    """Return (csharp_type, parser_expression) for an operation's 2xx body."""
    resp = success_response(op)
    content = (resp or {}).get("content") or {}
    # Prefer JSON; utoipa emits text/plain for primitives on older configs.
    body = content.get("application/json") or content.get("text/plain")
    if not body or not body.get("schema"):
        return "bool", "ApiParse.NoContent"

    # `Option<T>` arrives as oneOf[null, T]; the caller just wants T (possibly null).
    schema, _ = unwrap_nullable(body["schema"])
    types = type_names(schema)
    if "boolean" in types:
        return "bool", "ApiParse.Bool"
    if "$ref" in schema:
        name = ref_name(schema["$ref"])
        target = schemas.get(name) or {}
        if is_enum(target):
            return "string", "ApiParse.Raw"
        return name, f"ApiParse.Object<{name}>"
    if "string" in types:
        return "string", "ApiParse.Raw"
    if "integer" in types:
        return "int", "ApiParse.Int"
    # Anything else (arrays, inline objects) is handed back unparsed; JsonUtility
    # cannot deserialize a top-level array anyway.
    return "string", "ApiParse.Raw"


def request_binding(op, schemas):
    """Return (csharp_type|None, schema_name|None) for the request body."""
    content = ((op.get("requestBody") or {}).get("content")) or {}
    body = content.get("application/json")
    if not body or not body.get("schema"):
        return None
    schema, _ = unwrap_nullable(body["schema"])
    if "$ref" in schema:
        return ref_name(schema["$ref"])
    return None


def collect_operations(spec, schemas):
    ops = []
    for path, item in spec.get("paths", {}).items():
        for verb, op in item.items():
            if verb.lower() not in ("get", "put", "post", "delete", "patch"):
                continue
            oid = op.get("operationId")
            if not oid:
                die(f"{verb.upper()} {path} has no operationId. "
                    "The generator needs one per operation; add it in the backend.")
            tags = op.get("tags") or []
            if not tags:
                die(f"{verb.upper()} {path} (operationId={oid}) has no tag. "
                    "The generator groups methods by tag; add one in the backend.")
            rtype, parser = response_binding(op, schemas)
            ops.append(dict(
                path=path, verb=verb.upper(), oid=oid, tag=tags[0],
                method=pascal(oid), req=request_binding(op, schemas),
                rtype=rtype, parser=parser,
                summary=op.get("summary") or op.get("description") or "",
            ))

    # These two are what the whole client is keyed on; refuse to emit broken code.
    dupes = {o["oid"] for o in ops if [x["oid"] for x in ops].count(o["oid"]) > 1}
    if dupes:
        die(f"duplicate operationId(s) {sorted(dupes)} -- method names would collide. Fix the spec.")
    per_class = collections.defaultdict(list)
    for o in ops:
        per_class[o["tag"]].append(o)
    for tag, group in per_class.items():
        names = [o["method"] for o in group]
        clash = {n for n in names if names.count(n) > 1}
        if clash:
            die(f"tag {tag!r} has colliding method names {sorted(clash)}. Fix the spec.")
    return ops


def render_client(ops, ns):
    by_tag = collections.defaultdict(list)
    for o in ops:
        by_tag[o["tag"]].append(o)

    out = [
        "// <auto-generated> see ApiModels.cs. Do not edit by hand. </auto-generated>",
        "",
        "using System;",
        "using UnityEngine;",
        "",
        "#nullable enable",
        "",
        f"namespace {ns}",
        "{",
    ]
    first_class = True
    for tag in sorted(by_tag):
        if not first_class:
            out.append("")
        first_class = False
        out.append(f"    public static class {pascal(tag)}Api")
        out.append("    {")
        for i, o in enumerate(sorted(by_tag[tag], key=lambda x: x["method"])):
            if i:
                out.append("")
            if o["summary"]:
                out.append("        /// <summary>")
                for line in o["summary"].split("\n"):
                    out.append(f"        /// {line.strip()}")
                out.append("        /// </summary>")
            out.append(f'        /// <remarks>{o["verb"]} {o["path"]}</remarks>')
            cb = f'Action<ApiResult<{o["rtype"]}>>? onComplete = null'
            if o["req"]:
                out.append(f'        public static void {o["method"]}({o["req"]} body, {cb})')
                out.append("        {")
                out.append(f'            ApiDispatch.Send("{o["verb"]}", "{o["path"]}", '
                           f'JsonUtility.ToJson(body), {o["parser"]}, onComplete);')
            else:
                out.append(f'        public static void {o["method"]}({cb})')
                out.append("        {")
                out.append(f'            ApiDispatch.Send("{o["verb"]}", "{o["path"]}", '
                           f'null, {o["parser"]}, onComplete);')
            out.append("        }")
        out.append("    }")
    out.append("}")
    out.append("")
    return "\n".join(out)


RUNTIME = '''// <auto-generated> see ApiModels.cs. Do not edit by hand. </auto-generated>
//
// This file is the seam between generated code and your game. Nothing here
// knows about Mirror, UnityWebRequest or your server address -- you supply that
// by assigning ApiConfig.Transport once at startup:
//
//     ApiConfig.Transport = new UnityWebRequestApiTransport(EnvConfig.DatabaseAccessServer);
//
// Thread per-call state (a NetworkConnectionToClient, GameObjects, ...) by
// capturing it in the callback closure at the call site.

using System;
using UnityEngine;

#nullable enable

namespace {NS}
{
    /// <summary>A request the transport must perform. Path is server-relative.</summary>
    public readonly struct ApiRequest
    {
        public readonly string Method;
        public readonly string Path;
        public readonly string? Body;

        public ApiRequest(string method, string path, string? body)
        {
            Method = method;
            Path = path;
            Body = body;
        }
    }

    /// <summary>The raw outcome of a transport call, before any parsing.</summary>
    public readonly struct ApiRawResponse
    {
        public readonly bool Success;
        public readonly long StatusCode;
        public readonly string? Body;
        public readonly string? Error;

        public ApiRawResponse(bool success, long statusCode, string? body, string? error)
        {
            Success = success;
            StatusCode = statusCode;
            Body = body;
            Error = error;
        }

        public static ApiRawResponse Ok(long status, string? body) =>
            new ApiRawResponse(true, status, body, null);

        public static ApiRawResponse Failed(long status, string? error) =>
            new ApiRawResponse(false, status, null, error);
    }

    /// <summary>A parsed API outcome handed back to callers.</summary>
    public readonly struct ApiResult<T>
    {
        public readonly bool Success;
        public readonly long StatusCode;
        public readonly T Value;
        public readonly string? Error;

        public ApiResult(bool success, long statusCode, T value, string? error)
        {
            Success = success;
            StatusCode = statusCode;
            Value = value;
            Error = error;
        }
    }

    /// <summary>Implement this once, with UnityWebRequest or whatever you prefer.</summary>
    public interface IApiTransport
    {
        void Send(ApiRequest request, Action<ApiRawResponse> callback);
    }

    public static class ApiConfig
    {
        public static IApiTransport? Transport;
    }

    /// <summary>Body parsers. JsonUtility cannot handle bare primitives, hence the split.</summary>
    public static class ApiParse
    {
        public static bool Bool(string? body) =>
            bool.TryParse((body ?? string.Empty).Trim(), out bool v) && v;

        public static bool NoContent(string? body) => true;

        public static int Int(string? body) =>
            int.TryParse((body ?? string.Empty).Trim(), out int v) ? v : 0;

        public static string Raw(string? body) => body ?? string.Empty;

        public static T Object<T>(string? body) => JsonUtility.FromJson<T>(body ?? string.Empty);
    }

    public static class ApiDispatch
    {
        public static void Send<T>(
            string method,
            string path,
            string? body,
            Func<string?, T> parse,
            Action<ApiResult<T>>? onComplete)
        {
            IApiTransport? transport = ApiConfig.Transport;
            if (transport == null)
            {
                onComplete?.Invoke(new ApiResult<T>(
                    false, 0, default!, "ApiConfig.Transport has not been assigned."));
                return;
            }

            transport.Send(new ApiRequest(method, path, body), raw =>
            {
                if (onComplete == null)
                {
                    return;
                }

                if (!raw.Success)
                {
                    onComplete(new ApiResult<T>(
                        false, raw.StatusCode, default!, raw.Error ?? "request failed"));
                    return;
                }

                try
                {
                    onComplete(new ApiResult<T>(true, raw.StatusCode, parse(raw.Body), null));
                }
                catch (Exception e)
                {
                    onComplete(new ApiResult<T>(
                        false, raw.StatusCode, default!, "could not parse response: " + e.Message));
                }
            });
        }
    }
}
'''


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--input", required=True)
    ap.add_argument("--output", required=True,
                    help="path for the models file; siblings are written next to it")
    ap.add_argument("--namespace", required=True)
    args = ap.parse_args()

    with open(args.input, encoding="utf-8") as fh:
        spec = json.load(fh)

    schemas = spec.get("components", {}).get("schemas", {})
    if not schemas:
        die("no components.schemas found in the spec")

    out = [
        "// <auto-generated>",
        f"//     Generated from {os.path.basename(args.input)} by Tools/openapi_to_unity.py",
        "//     Run ./openapi_generator.sh to regenerate. Do not edit by hand.",
        "// </auto-generated>",
        "",
        "using System;",
        "using System.Collections.Generic;",
        "",
        "#nullable enable",
        "",
        f"namespace {args.namespace}",
        "{",
    ]

    enums = sorted(n for n, s in schemas.items() if is_enum(s))
    classes = sorted(n for n, s in schemas.items() if not is_enum(s))

    blocks = [render_enum(n, schemas[n]) for n in enums]
    blocks += [render_class(n, schemas[n], schemas) for n in classes]

    for i, block in enumerate(blocks):
        out.extend(block)
        if i != len(blocks) - 1:
            out.append("")

    out.append("}")
    out.append("")

    outdir = os.path.dirname(os.path.abspath(args.output))
    os.makedirs(outdir, exist_ok=True)
    with open(args.output, "w", encoding="utf-8") as fh:
        fh.write("\n".join(out))

    ops = collect_operations(spec, schemas)
    runtime_path = os.path.join(outdir, "ApiRuntime.cs")
    with open(runtime_path, "w", encoding="utf-8") as fh:
        fh.write(RUNTIME.replace("{NS}", args.namespace))
    client_path = os.path.join(outdir, "ApiClient.cs")
    with open(client_path, "w", encoding="utf-8") as fh:
        fh.write(render_client(ops, args.namespace))

    tags = sorted({o["tag"] for o in ops})
    print(f"Generated {len(enums)} enum constant classes and {len(classes)} DTOs")
    print(f"  -> {args.output}")
    print(f"Generated transport seam (IApiTransport, ApiResult, ApiDispatch)")
    print(f"  -> {runtime_path}")
    print(f"Generated {len(ops)} endpoint methods across {len(tags)} classes: "
          + ", ".join(pascal(t) + "Api" for t in tags))
    print(f"  -> {client_path}")


if __name__ == "__main__":
    main()
