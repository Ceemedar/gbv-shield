using System;

namespace GBV_Shield.Services
{
    public class Story
    {
        public string Author { get; set; } = "Anonymous";
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
