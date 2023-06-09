using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Brain.OpenAI
{
    /// <summary>
    /// Schema for Request and Response messages to and from OpenAI endpoints
    /// </summary>
    public class Schema
    {
        /// <summary>
        /// For deserializing data attribute that contains mutliple attributes
        /// </summary>
        public class DataSchema
        {
            public Data[] data { get; set; }
        }
        /// <summary>
        /// For deserializing choice attribute that contains mutliple attributes
        /// </summary>
        public class ChoicesSchema
        {
            public Choice[] choices { get; set; }
        }
        /// <summary>
        /// data attribute
        /// </summary>
        public class Data
        {
            public string id { get; set; }
            public string url { get; set; }
            public string b64_json { get; set; }
            public List<float> embedding { get; set; }
            public string filename { get; set; }
            public bool deleted { get; set; }
        }
        /// <summary>
        /// choice attribute
        /// </summary>
        public class Choice
        {
            public Msg message { get; set; }
            public string text { get; set; }
        }
        /// <summary>
        /// message attribute
        /// </summary>
        public class Msg
        {
            public Msg(string role, string content)
            {
                this.role = role;
                this.content = content;
            }

            public string role { get; set; }
            public string content { get; set; }
        }
        /// <summary>
        /// A common schema for serializing all requests to OpenAI endpoints
        /// </summary>
        public class ReqSchema
        {
            public string model { get; set; }
            public string input { get; set; }
            public string instruction { get; set; }
            public string image { get; set; }
            public string mask { get; set; }
            public string prompt { get; set; }
            public string suffix { get; set; }
            public Msg[] messages { get; set; }
            public double? temperature { get; set; }
            public double? top_p { get; set; }
            public int? n { get; set; }
            public string size { get; set; }
            public string response_format { get; set; }
            public int? logprobs { get; set; }
            public bool? echo { get; set; }
            //public bool? stream { get; set; } not implemented due to complexity of expiration cycles
            public string stop { get; set; }
            public int? max_tokens { get; set; }
            public double? presence_penalty { get; set; }
            public double? frequency_penalty { get; set; }
            //public JsonObject? logit_bias { get; set; } not implemented due to complexity json in C#
            public int? best_of { get; set; }
            public string user { get; set; }
        }
        /// <summary>
        /// common schema for serializing content for HttpClient
        /// </summary>
        public class ReqContent
        {
            public ReqContent(byte[] byteData, string fieldName, string fileName)
            {
                this.byteData = byteData;
                this.fieldName = fieldName;
                this.fileName = fileName;
            }
            public byte[] byteData { get; set; }
            public string fieldName { get; set; }
            public string fileName { get; set; }
        }
    }
}
