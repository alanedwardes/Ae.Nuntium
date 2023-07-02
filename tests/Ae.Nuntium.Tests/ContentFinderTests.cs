﻿using Ae.Nuntium.Destinations;
using Ae.Nuntium.Extractors;
using Ae.Nuntium.Sources;
using Ae.Nuntium.Trackers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Ae.Nuntium.Tests
{
    public sealed class ContentFinderTests : IDisposable
    {
        private readonly MockRepository _repository = new(MockBehavior.Strict);

        public void Dispose() => _repository.VerifyAll();

        [Fact]
        public async Task FindContentNoPosts()
        {
            var finder = new ContentFinder(NullLogger<ContentFinder>.Instance);

            var source = _repository.Create<IContentSource>();
            var extractor = _repository.Create<IPostExtractor>();
            var tracker = _repository.Create<ILinkTracker>();
            var destination = _repository.Create<IExtractedPostDestination>();

            var sourceDocument = new SourceDocument();
            source.Setup(x => x.GetContent(CancellationToken.None))
                .ReturnsAsync(sourceDocument);

            extractor.Setup(x => x.ExtractPosts(sourceDocument))
                .ReturnsAsync(new List<ExtractedPost>());

            await finder.FindContent(source.Object, extractor.Object, tracker.Object, new[] { destination.Object }, CancellationToken.None);
        }

        [Fact]
        public async Task FindContent()
        {
            var finder = new ContentFinder(NullLogger<ContentFinder>.Instance);

            var source = _repository.Create<IContentSource>();
            var extractor = _repository.Create<IPostExtractor>();
            var tracker = _repository.Create<ILinkTracker>();
            var destination = _repository.Create<IExtractedPostDestination>();

            var sourceDocument = new SourceDocument();
            source.Setup(x => x.GetContent(CancellationToken.None))
                  .ReturnsAsync(sourceDocument);

            var post1 = new ExtractedPost(new Uri("https://www.example.com/"));
            var post2 = new ExtractedPost(new Uri("https://www.example.org/"));
            var post3 = new ExtractedPost(new Uri("https://www.example.net/"));

            extractor.Setup(x => x.ExtractPosts(sourceDocument))
                     .ReturnsAsync(new[] { post1, post2, post3 });

            // Get unseen links - treat result as a set, order not guaranteed
            tracker.Setup(x => x.GetUnseenLinks(new[] { post1.Permalink, post2.Permalink, post3.Permalink }, CancellationToken.None))
                   .ReturnsAsync(new[] { post3.Permalink, post1.Permalink }); // post 2 was seen

            // Ensure posts are shared in descending order to which they were receieved from the source
            destination.Setup(x => x.ShareExtractedPosts(new[] { post3, post1 }, CancellationToken.None))
                       .Returns(Task.CompletedTask);

            tracker.Setup(x => x.SetLinksSeen(new[] { post3.Permalink, post1.Permalink }, CancellationToken.None))
                   .Returns(Task.CompletedTask);

            await finder.FindContent(source.Object, extractor.Object, tracker.Object, new[] { destination.Object }, CancellationToken.None);
        }

        [Fact]
        public async Task FindContentAllSeen()
        {
            var finder = new ContentFinder(NullLogger<ContentFinder>.Instance);

            var source = _repository.Create<IContentSource>();
            var extractor = _repository.Create<IPostExtractor>();
            var tracker = _repository.Create<ILinkTracker>();
            var destination = _repository.Create<IExtractedPostDestination>();

            var sourceDocument = new SourceDocument();
            source.Setup(x => x.GetContent(CancellationToken.None))
                  .ReturnsAsync(sourceDocument);

            var post1 = new ExtractedPost(new Uri("https://www.example.com/"));
            var post2 = new ExtractedPost(new Uri("https://www.example.org/"));
            var post3 = new ExtractedPost(new Uri("https://www.example.net/"));

            extractor.Setup(x => x.ExtractPosts(sourceDocument))
                     .ReturnsAsync(new[] { post1, post2, post3 });

            tracker.Setup(x => x.GetUnseenLinks(new[] { post1.Permalink, post2.Permalink, post3.Permalink }, CancellationToken.None))
                   .ReturnsAsync(Enumerable.Empty<Uri>());

            await finder.FindContent(source.Object, extractor.Object, tracker.Object, new[] { destination.Object }, CancellationToken.None);
        }
    }
}