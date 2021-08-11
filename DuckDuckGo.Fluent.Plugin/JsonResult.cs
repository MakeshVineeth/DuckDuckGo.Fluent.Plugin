using System.Collections.Generic;

namespace DuckDuckGo.Fluent.Plugin
{
    public record JsonResult
    {
        public class Topic
        {
            public string FirstUrl { get; set; }
            public string Text { get; set; }
        }

        public record RelatedTopic
        {
            public string FirstUrl { get; set; }
            public string Text { get; set; }
            public string Name { get; set; }
            public List<Topic> Topics { get; set; }
        }


        public record DuckDuckGoApiResult
        {
            public string AbstractText { get; set; }
            public string AbstractUrl { get; set; }
            public string Answer { get; set; }
            public string AnswerType { get; set; }
            public string Definition { get; set; }
            public string DefinitionUrl { get; set; }
            public List<RelatedTopic> RelatedTopics { get; set; }
            public List<RelatedTopic> Results { get; set; }
        }
    }
}
