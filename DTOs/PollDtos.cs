namespace NewsPortalPro.DTOs
{
    public class PollDto
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalVotes { get; set; }
        public List<PollOptionDto> Options { get; set; } = [];
        public bool HasVoted { get; set; }
        public int? UserVotedOptionId { get; set; }
    }

    public class PollOptionDto
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int VoteCount { get; set; }
        public double Percentage { get; set; }
    }

    public class CreatePollDto
    {
        public string Question { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public List<string> Options { get; set; } = [];
    }

    public class VoteDto
    {
        public int PollId { get; set; }
        public int OptionId { get; set; }
    }
}