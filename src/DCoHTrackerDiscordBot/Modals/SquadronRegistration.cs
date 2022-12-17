using Discord.Interactions;

namespace DCoHTrackerDiscordBot.Modals
{
    public class SquadronRegistration : IModal
    {
        public string Title => "Squadron Registration";
        [InputLabel("Squadron Id")]
        [RequiredInput(true)]
        [ModalTextInput("SquadronId", TextInputStyle.Short, "Your 4 letter Squadron Id", 4, 4)]
        public string? SquadronId { get; set; }

        [InputLabel("Squadron Name")]
        [RequiredInput(true)]
        [ModalTextInput("SquadronName", TextInputStyle.Short, "Full squadron name", 4, 64)]
        public string? SquadronName { get; set; }
    }
}
