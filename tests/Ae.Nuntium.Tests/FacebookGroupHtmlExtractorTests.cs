using Ae.Nuntium.Extractors;
using Ae.Nuntium.Sources;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ae.Nuntium.Tests;

public sealed class FacebookGroupHtmlExtractorTests
{
    [Fact]
    public async Task ExtractPosts1()
    {
        var extractor = new FacebookGroupHtmlExtractor(NullLogger<FacebookGroupHtmlExtractor>.Instance);

        var posts = await extractor.ExtractPosts(new SourceDocument { Body = File.ReadAllText("Files/group1.html") });

        posts.Compare("Files/group1.json");
    }

    [Fact]
    public async Task ExtractPosts2()
    {
        var extractor = new FacebookGroupHtmlExtractor(NullLogger<FacebookGroupHtmlExtractor>.Instance);

        var posts = await extractor.ExtractPosts(new SourceDocument { Body = File.ReadAllText("Files/group2.html") });

        posts.Compare("Files/group2.json");
    }

    [Fact]
    public async Task ExtractPosts3()
    {
        var extractor = new FacebookGroupHtmlExtractor(NullLogger<FacebookGroupHtmlExtractor>.Instance);

        var posts = await extractor.ExtractPosts(new SourceDocument { Body = File.ReadAllText("Files/group3.html") });

        posts.Compare("Files/group3.json");
    }

    [Fact]
    public async Task ExtractPosts4()
    {
        var extractor = new FacebookGroupHtmlExtractor(NullLogger<FacebookGroupHtmlExtractor>.Instance);

        var posts = await extractor.ExtractPosts(new SourceDocument { Body = File.ReadAllText("Files/group4.html") });

        posts.Compare("Files/group4.json");
    }

    [Fact]
    public async Task ExtractPostsInvalidHtml()
    {
        var extractor = new FacebookGroupHtmlExtractor(NullLogger<FacebookGroupHtmlExtractor>.Instance);

        var posts = await extractor.ExtractPosts(new SourceDocument { Body = File.ReadAllText("Files/tweets1.html") });

        Assert.Empty(posts);
    }
}