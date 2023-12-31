﻿using Ae.Nuntium.Sources;
using HtmlAgilityPack;

namespace Ae.Nuntium.Extractors
{
    public sealed class TwitterHtmlExtractor : IPostExtractor
    {
        public Task<IList<ExtractedPost>> ExtractPosts(SourceDocument sourceDocument)
        {
            var extractedPosts = new List<ExtractedPost>();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(sourceDocument.Body);

            var tweets = htmlDoc.DocumentNode.SelectNodes(".//article[@data-testid = 'tweet']");

            foreach (var tweet in tweets ?? Enumerable.Empty<HtmlNode>())
            {
                tweet.MakeRelativeUrisAbsolute(sourceDocument.Address);

                // Replace all emoji images with a span element + the emoji characters
                foreach (var node in tweet.DescendantsAndSelf())
                {
                    if (node.Name == "img")
                    {
                        var src = node.GetAttributeValue("src", string.Empty);
                        if (!src.Contains("emoji") && !src.EndsWith(".svg"))
                        {
                            continue;
                        }

                        var alt = node.GetAttributeValue("alt", string.Empty);
                        if (!string.IsNullOrEmpty(alt))
                        {
                            node.Name = "span";
                            node.InnerHtml = alt;
                            node.Attributes.RemoveAll();
                        }
                    }
                }

                var links = new HashSet<Uri>();
                var media = new HashSet<Uri>();

                tweet.GetLinksAndMedia(sourceDocument.Address, link => links.Add(link), mediaUri =>
                {
                    if (!mediaUri.PathAndQuery.Contains("/profile_images/", StringComparison.InvariantCultureIgnoreCase) &&
                        !mediaUri.PathAndQuery.Contains("/hashflags/", StringComparison.InvariantCultureIgnoreCase) &&
                        !mediaUri.PathAndQuery.Contains("/emoji/", StringComparison.InvariantCultureIgnoreCase))
                    {
                        media.Add(mediaUri);
                    }
                });

                var permalinks = links.Where(x => sourceDocument.Address.IsBaseOf(x) && x.PathAndQuery.Contains("/status/") && !x.PathAndQuery.EndsWith("/analytics"));
                if (!permalinks.Any())
                {
                    // This is invalid if there are no appropriate permalinks
                    continue;
                }

                var extractedPost = new ExtractedPost(permalinks.First())
                {
                    Links = links,
                    Media = media,
                };

                var content = tweet.SelectSingleNode(".//div[@data-testid = 'tweetText']");
                if (content != null)
                {
                    extractedPost.Body = content.InnerHtml;
                }

                var author = tweet.SelectSingleNode(".//div[@data-testid = 'User-Name']");
                if (author != null)
                {
                    var spans = author.SelectNodes(".//span");
                    if (spans.Any())
                    {
                        extractedPost.Author = spans.First().InnerText;
                    }
                }

                var avatar = tweet.SelectSingleNode(".//div[@data-testid = 'Tweet-User-Avatar']");
                if (avatar != null)
                {
                    if (avatar.SelectSingleNode(".//img").TryGetAbsoluteUriFromAttribute("src", out var avatarUri))
                    {
                        extractedPost.Avatar = avatarUri;
                    }
                }

                var time = tweet.SelectSingleNode(".//time");
                if (time != null)
                {
                    var datetime = time.GetAttributeValue<string>("datetime", null);
                    if (datetime != null && DateTimeOffset.TryParse(datetime, out var resultingDateTime))
                    {
                        extractedPost.Published = resultingDateTime.UtcDateTime;
                    }
                }

                extractedPosts.Add(extractedPost);
            }

            return Task.FromResult<IList<ExtractedPost>>(extractedPosts);
        }
    }
}
