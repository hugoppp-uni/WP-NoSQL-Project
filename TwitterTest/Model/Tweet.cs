using System;
using Tweetinvi.Models.V2;

namespace TwitterTest.Model
{
    public class Tweet
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Language { get; set; }
        
        public DateTimeOffset CreatedAt  { get; set; }
        
        public String AuthorID  { get; set; }

        public Tweet(string id, string text, string language, DateTimeOffset createdAt, string authorId)
        {
            Id = id;
            Text = text;
            Language = language;
            CreatedAt = createdAt;
            AuthorID = authorId;
        }
    }
    
}