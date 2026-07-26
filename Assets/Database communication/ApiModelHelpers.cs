using System;

#nullable enable

namespace FishyGame.Api
{
    // HAND-WRITTEN companion to Generated/ApiModels.cs.
    //
    // The generated DTOs mirror the wire format exactly: ids are strings, times
    // are ISO 8601 strings. These partials add the typed accessors the game used
    // to get from the hand-written UserData struct in items/userDataStruct.cs,
    // which this file replaced.
    //
    // Safe from regeneration: the generator only ever rewrites ApiModels.cs,
    // ApiRuntime.cs and ApiClient.cs.

    public partial class UserData
    {
        /// <summary>Id of the last completed Herb quest, Guid.Empty when never completed.</summary>
        public Guid LastCompletedHerbQuestId =>
            string.IsNullOrEmpty(last_completed_herb_quest_id)
                ? Guid.Empty
                : Guid.Parse(last_completed_herb_quest_id);

        /// <summary>Id of the last accepted Herb quest, Guid.Empty when never accepted.</summary>
        public Guid LastAcceptedHerbQuestId =>
            string.IsNullOrEmpty(last_accepted_herb_quest_id)
                ? Guid.Empty
                : Guid.Parse(last_accepted_herb_quest_id);

        public Guid? SelectedRod =>
            string.IsNullOrEmpty(selected_rod) ? null : Guid.Parse(selected_rod);

        public Guid? SelectedBait =>
            string.IsNullOrEmpty(selected_bait) ? null : Guid.Parse(selected_bait);
    }

    public partial class ActiveEffect
    {
        public DateTime ExpiryTime => DateTimeOffset.Parse(expiry_time).UtcDateTime;
    }

    public partial class InventoryItem
    {
        public Guid ItemUuid =>
            string.IsNullOrEmpty(item_uuid) ? Guid.Empty : Guid.Parse(item_uuid);
    }

    public partial class MailEntry
    {
        public Guid MailID => string.IsNullOrEmpty(mail_id) ? Guid.Empty : Guid.Parse(mail_id);
    }

    public partial class Friend
    {
        public string friendName => friend_name;

        public Guid friendId => string.IsNullOrEmpty(friend_id) ? Guid.Empty : Guid.Parse(friend_id);
    }

    public partial class FriendRequest
    {
        public Guid otherId => string.IsNullOrEmpty(other_id) ? Guid.Empty : Guid.Parse(other_id);

        public string otherName => other_name;

        public Guid RequestSenderId =>
            string.IsNullOrEmpty(request_sender_id) ? Guid.Empty : Guid.Parse(request_sender_id);
    }
}
