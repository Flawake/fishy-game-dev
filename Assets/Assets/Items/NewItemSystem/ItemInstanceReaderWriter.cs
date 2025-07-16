// ItemInstanceReaderWriter.cs
using Mirror;
using System;

namespace NewItemSystem {
    public static class ItemInstanceReaderWriter {
        // Mirror uses the method name pattern WriteX to auto-register.
        public static void WriteItemInstance(this NetworkWriter writer, ItemInstance item) {
            writer.WriteInt(item.def.Id);
            writer.WriteGuid(item.uuid);
            byte[] blob = StatePacker.Pack(item.state);
            writer.WriteBytesAndSize(blob);
        }

        // Mirror uses the method name pattern ReadX to auto-register.
        public static ItemInstance ReadItemInstance(this NetworkReader reader) {
            int defId = reader.ReadInt();
            Guid uuid = reader.ReadGuid();
            byte[] blob = reader.ReadBytesAndSize();

            var def = ItemRegistry.Get(defId);
            var inst = new ItemInstance(def) { uuid = uuid };
            StatePacker.UnpackInto(blob, inst.state);
            return inst;
        }
    }
} 