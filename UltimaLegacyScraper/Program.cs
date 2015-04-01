using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace UltimaLegacyScraper {
	class Program {
		private static string _baseUrl = "http://www.ultimalegacy.net/archive/";

		static void Main(string[] args) {

			var basePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Results");

			if (!Directory.Exists(basePath)) {
				// Create the folder and write the stylesheet there
				Directory.CreateDirectory(basePath);
				File.WriteAllText(Path.Combine(basePath, "Style.css"), Resources.Style);
			}

			var wc = new WebClient();

			// Loop through a lot of identifiers!
			for (int i = 0; i < 10000; i++) {
				// Download html
				var html = wc.DownloadString(_baseUrl + "viewstory.php?sid=" + i);

				// Check if it contains an error
				if (html.Contains("A fatal MySQL error was encountered"))
					continue;

				var doc = new HtmlDocument();
				doc.LoadHtml(html);

				// Extract the title
				var titleElement = doc.DocumentNode.Descendants("title").FirstOrDefault();
				var title = "Missing title";
				if (titleElement != null)
					title = titleElement.InnerText;

				// Find the story element
				var storyElement = doc.GetElementbyId("story");

				if (storyElement == null)
					continue;

				// Find all images in the story
				var images = storyElement.Descendants("img");
				foreach (var image in images) {
					var src = image.GetAttributeValue("src", null);
					if (string.IsNullOrEmpty(src))
						continue;

					// Check if it's a relative url and fix it if so
					if (!src.StartsWith("http://") && !src.StartsWith("https://"))
						src = _baseUrl + src;

					// Put this in a try-catch if any image is missing
					try {
						// Download image
						var buffer = wc.DownloadData(src);

						// Inline the image
						image.SetAttributeValue("src", "data:image/jpeg;base64," + Convert.ToBase64String(buffer));
					} catch (Exception) {
						/* Here there be dragons */
					}
				}

				// Get the story html with updated images
				var story = storyElement.InnerHtml;

				// Load template and replace values
				var template = Resources.Template;
				template = template.Replace("{{Title}}", title);
				template = template.Replace("{{Story}}", story);

				var filename = title + ".html";
				foreach (var c in Path.GetInvalidFileNameChars()) {
					filename = filename.Replace(c.ToString(), string.Empty); // Convert char to string to we can replace with string.Empty
				}

				// Write the template to disk
				File.WriteAllText(Path.Combine(basePath, filename), template);
			}
		}
	}
}
