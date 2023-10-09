using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TryJson
{
    class Try
    {
        static void TryReplace()
        {
            string json = @"{
                  'channel': {
                    'title': 'Star Wars',
                    'link': 'http://www.starwars.com',
                    'description': 'Star Wars blog.',
                    'obsolete': 'Obsolete value',
                    'item': []
                  }
                }";

            JObject rss = JObject.Parse(json);

            JObject channel = (JObject)rss["channel"];

            JToken title = channel["title"];
            title = ((string)channel["title"]).ToUpper();

            channel["description"] = ((string)channel["description"]).ToUpper();

            // 替換方法 1
            //channel["title"] = ((string)channel["title"]).ToUpper();

            // 替換方法 2
            var jToken = rss.SelectToken("channel.title");
            JValue jval = (JValue)jToken;
            jval.Value = jval.Value.ToString().ToUpper();

            channel.Property("obsolete").Remove();
            channel.Property("description").AddAfterSelf(new JProperty("new", "New value"));

            JArray item = (JArray)channel["item"];
            item.Add("Item 1");
            item.Add("Item 2");

            Console.WriteLine(rss.ToString());
            Console.ReadLine();
        }

    }
}
