using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebAutomation.Helper {
	public static class WebcamCloudAnalyzer {
		/// <summary>
		/// Downloads the given page, finds the image element with id "premium_webcam_static",
		/// downloads the image and estimates cloud coverage as a scale 1..10 (1 = clear, 10 = fully covered).
		/// </summary>
		public static async Task<int> GetCloudScaleFromPageUrl(string pageUrl) {
			if(string.IsNullOrWhiteSpace(pageUrl))
				throw new ArgumentNullException(nameof(pageUrl));

			string html;
			using(var wc = new WebClient()) {
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
				wc.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (compatible)";
				html = await wc.DownloadStringTaskAsync(pageUrl).ConfigureAwait(false);
			}

			string imgUrl = ExtractImageUrl(html);
			if(string.IsNullOrEmpty(imgUrl))
				throw new InvalidOperationException("Could not find webcam image with id 'premium_webcam_static'.");

			if(!imgUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
				imgUrl = new Uri(new Uri(pageUrl), imgUrl).ToString();
			}

			byte[] imageData;
			using(var wc = new WebClient()) {
				wc.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (compatible)";
				imageData = await wc.DownloadDataTaskAsync(imgUrl).ConfigureAwait(false);
			}

			// save image to cache folder if possible
			try {
				string cacheDir = @"C:\Projekte\WebAutomation.Site\images\cache";
				try { Directory.CreateDirectory(cacheDir); } catch { }
				string ext = null;
				try {
					var uri = new Uri(imgUrl);
					ext = Path.GetExtension(uri.AbsolutePath);
				} catch { }
				if(string.IsNullOrEmpty(ext))
					ext = ".jpg";
				string fileName = $"cloud{ext}";
				string fullPath = Path.Combine(cacheDir, fileName);
				try { File.WriteAllBytes(fullPath, imageData); } catch { }
			} catch { }

			using(var ms = new MemoryStream(imageData))
			using(var bmp = new Bitmap(ms)) {
				double cloudRatio = AnalyzeBitmapForClouds(bmp);
				int scale = Math.Max(1, (int)Math.Round(cloudRatio * 10.0));
				if(scale < 1)
					scale = 1;
				if(scale > 10)
					scale = 10;
				return scale;
			}
		}

		private static string ExtractImageUrl(string html) {
			if(string.IsNullOrEmpty(html))
				return null;

			// Try to find <img ... id="premium_webcam_static" ... src="..." />
			var regex1 = new Regex("<img[^>]*id\\s*=\\s*['\"]premium_webcam_static['\"][^>]*src\\s*=\\s*['\"]([^'\"]+)['\"]", RegexOptions.IgnoreCase);
			var m = regex1.Match(html);
			if(m.Success && m.Groups.Count > 1)
				return WebDecode(m.Groups[1].Value);

			// Or src before id
			var regex2 = new Regex("<img[^>]*src\\s*=\\s*['\"]([^'\"]+)['\"][^>]*id\\s*=\\s*['\"]premium_webcam_static['\"]", RegexOptions.IgnoreCase);
			m = regex2.Match(html);
			if(m.Success && m.Groups.Count > 1)
				return WebDecode(m.Groups[1].Value);

			return null;
		}

		private static string WebDecode(string s) {
			if(string.IsNullOrEmpty(s))
				return s;
			return WebUtility.HtmlDecode(s).Trim();
		}

		private static double AnalyzeBitmapForClouds(Bitmap bmp) {
			// Sample pixels with a stride to keep performance reasonable on big images
			int stride = Math.Max(1, Math.Min(8, Math.Max(1, bmp.Width / 200)));
			long cloudCount = 0;
			long total = 0;

			for(int y = 0; y < bmp.Height; y += stride) {
				for(int x = 0; x < bmp.Width; x += stride) {
					Color c = bmp.GetPixel(x, y);
					total++;
					double h, s, v;
					RgbToHsv(c.R, c.G, c.B, out h, out s, out v);

					// Heuristic: clouds are pixels with relatively high brightness (v) and low saturation (s)
					if(v >= 0.55 && s <= 0.45) {
						// count as cloud
						cloudCount++;
						continue;
					}

					// Also count medium bright low saturation as partial cloud
					if(v >= 0.45 && s <= 0.3)
						cloudCount++;
				}
			}

			if(total == 0)
				return 0.0;
			return (double)cloudCount / (double)total;
		}

		// Convert RGB (0-255) to HSV (h 0..360, s 0..1, v 0..1)
		private static void RgbToHsv(int r, int g, int b, out double h, out double s, out double v) {
			double rd = r / 255.0;
			double gd = g / 255.0;
			double bd = b / 255.0;

			double max = new[] { rd, gd, bd }.Max();
			double min = new[] { rd, gd, bd }.Min();
			v = max;
			double delta = max - min;
			if(max == 0) {
				s = 0;
				h = 0;
				return;
			}
			s = (max == 0) ? 0 : delta / max;
			if(delta == 0) {
				h = 0;
			} else if(max == rd) {
				h = 60 * (((gd - bd) / delta) % 6);
			} else if(max == gd) {
				h = 60 * (((bd - rd) / delta) + 2);
			} else {
				h = 60 * (((rd - gd) / delta) + 4);
			}
			if(h < 0)
				h += 360;
		}
	}
}
