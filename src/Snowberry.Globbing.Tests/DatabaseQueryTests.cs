namespace Snowberry.Globbing.Tests;

/// <summary>
/// Comprehensive tests for database query use cases.
/// These tests demonstrate how to generate regex patterns for database queries.
/// </summary>
public class DatabaseQueryTests
{
    [Fact]
    public void DatabaseQuery_SimpleFileExtension_GeneratesUsablePattern()
    {
        // For SQL: SELECT * FROM files WHERE filename REGEXP '<pattern>'
        var regex = GlobMatcher.MakeRe("*.js");
        string pattern = regex.ToString();

        Assert.NotNull(pattern);
        Assert.NotEmpty(pattern);

        // Test the pattern works
        Assert.Matches(regex, "test.js");
        Assert.Matches(regex, "app.js");
        Assert.DoesNotMatch(regex, "test.ts");
    }

    [Fact]
    public void DatabaseQuery_MultipleExtensions_UsingBraces()
    {
        // Pattern for: SELECT * FROM files WHERE filename REGEXP '<pattern>'
        var regex = GlobMatcher.MakeRe("*.{js,ts,tsx}");
        string pattern = regex.ToString();

        Assert.NotNull(pattern);

        // Verify it matches all specified extensions
        Assert.Matches(regex, "app.js");
        Assert.Matches(regex, "app.ts");
        Assert.Matches(regex, "Component.tsx");
        Assert.DoesNotMatch(regex, "style.css");
    }

    [Fact]
    public void DatabaseQuery_DirectoryPath_WithGlobstar()
    {
        // For: SELECT * FROM files WHERE path REGEXP '<pattern>'
        var regex = GlobMatcher.MakeRe("src/**/*.js");
        string pattern = regex.ToString();

        Assert.NotNull(pattern);

        Assert.Matches(regex, "src/app.js");
        Assert.Matches(regex, "src/components/Button.js");
        Assert.Matches(regex, "src/utils/helpers/format.js");
        Assert.DoesNotMatch(regex, "lib/test.js");
    }

    [Fact]
    public void DatabaseQuery_CaseInsensitive_ForWindowsFilenames()
    {
        // For case-insensitive matching in databases
        var regex = GlobMatcher.MakeRe("*.PDF", new GlobbingOptions { NoCase = true });
        string pattern = regex.ToString();

        Assert.NotNull(pattern);

        Assert.Matches(regex, "document.pdf");
        Assert.Matches(regex, "document.PDF");
        Assert.Matches(regex, "document.Pdf");
    }

    [Fact]
    public void DatabaseQuery_Contains_MatchAnywhere()
    {
        // For: WHERE filename LIKE '%test%'  -> using regex
        var regex = GlobMatcher.MakeRe("*test*", new GlobbingOptions { Contains = true });
        string pattern = regex.ToString();

        Assert.NotNull(pattern);

        Assert.Matches(regex, "mytest.js");
        Assert.Matches(regex, "test-file.js");
        Assert.Matches(regex, "file.test.js");
    }

    [Fact]
    public void DatabaseQuery_ExcludePattern_UsingNegation()
    {
        // For: WHERE NOT filename REGEXP '<pattern>'
        string negatedPattern = "!*.test.js";
        bool isTestFile = GlobMatcher.IsMatch("app.test.js", negatedPattern);
        bool isNormalFile = GlobMatcher.IsMatch("app.js", negatedPattern);

        Assert.False(isTestFile); // Should not match (excluded)
        Assert.True(isNormalFile); // Should match
    }

    [Fact]
    public void DatabaseQuery_ComplexPath_ProductionCode()
    {
        // Match production code only - demonstrating separate include/exclude patterns
        string includePattern = "src/**/*.{js,ts}";
        string excludeTest = "!**/*.test.{js,ts}";

        Assert.True(GlobMatcher.IsMatch("src/app.js", includePattern));
        Assert.True(GlobMatcher.IsMatch("src/components/Button.ts", includePattern));

        // Test files should match the include but be excluded
        Assert.True(GlobMatcher.IsMatch("src/app.test.js", includePattern));
        Assert.False(GlobMatcher.IsMatch("src/app.test.js", excludeTest)); // Excluded
    }

    [Fact]
    public void DatabaseQuery_MongoDBRegex_GeneratePattern()
    {
        // For MongoDB: db.collection.find({ filename: { $regex: /pattern/ } })
        var regex = GlobMatcher.MakeRe("*.{jpg,png,gif}");
        string pattern = regex.ToString();

        Assert.NotNull(pattern);

        // Test it works for image files
        Assert.Matches(regex, "photo.jpg");
        Assert.Matches(regex, "image.png");
        Assert.Matches(regex, "animation.gif");
        Assert.DoesNotMatch(regex, "video.mp4");
    }

    [Fact]
    public void DatabaseQuery_PostgreSQL_RegexOperator()
    {
        // For PostgreSQL: SELECT * FROM files WHERE filename ~ '<pattern>'
        var regex = GlobMatcher.MakeRe("data-*.csv");
        string pattern = regex.ToString();

        Assert.NotNull(pattern);
        Assert.NotEmpty(pattern);

        Assert.Matches(regex, "data-2024.csv");
        Assert.Matches(regex, "data-export.csv");
        Assert.DoesNotMatch(regex, "report.csv");
    }

    [Fact]
    public void DatabaseQuery_ElasticSearch_WildcardQuery()
    {
        // For Elasticsearch wildcard queries
        var regex = GlobMatcher.MakeRe("log-*.txt");
        string pattern = regex.ToString();

        Assert.NotNull(pattern);

        Assert.Matches(regex, "log-2024-01-01.txt");
        Assert.Matches(regex, "log-error.txt");
        Assert.DoesNotMatch(regex, "data.txt");
    }

    [Fact]
    public void DatabaseQuery_VersionedFiles_NumericPattern()
    {
        // Match versioned files like file.v1.js, file.v2.js
        var regex = GlobMatcher.MakeRe("*.v[0-9].js");
        string pattern = regex.ToString();

        Assert.NotNull(pattern);

        Assert.Matches(regex, "app.v1.js");
        Assert.Matches(regex, "app.v5.js");
    }

    [Fact]
    public void DatabaseQuery_DatePattern_YYYYMMDD()
    {
        // Match files with date pattern: backup-20240101.sql
        var regex = GlobMatcher.MakeRe("backup-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9].sql");
        string pattern = regex.ToString();

        Assert.NotNull(pattern);

        Assert.Matches(regex, "backup-20240101.sql");
        Assert.Matches(regex, "backup-20231231.sql");
    }

    [Fact]
    public void DatabaseQuery_PatternStorage_AndRecreation()
    {
        // Store pattern string in database, recreate regex later
        var regex = GlobMatcher.MakeRe("**/*.{js,ts}");
        string storedPattern = regex.ToString();

        // Later, recreate from stored pattern
        var recreatedRegex = new System.Text.RegularExpressions.Regex(storedPattern, regex.Options);

        Assert.Matches(recreatedRegex, "src/app.js");
        Assert.Matches(recreatedRegex, "lib/utils.ts");
    }

    [Fact]
    public void DatabaseQuery_MultipleConditions_WithOR()
    {
        // Multiple patterns (OR condition)
        string[] patterns = ["*.js", "*.ts", "*.tsx"];

        Assert.True(GlobMatcher.IsMatch("app.js", patterns));
        Assert.True(GlobMatcher.IsMatch("app.ts", patterns));
        Assert.True(GlobMatcher.IsMatch("Component.tsx", patterns));
        Assert.False(GlobMatcher.IsMatch("style.css", patterns));
    }

    [Fact]
    public void DatabaseQuery_PathNormalization_UnixStyle()
    {
        // Normalize Windows paths to Unix for consistent database storage
        var options = new GlobbingOptions { Windows = false };
        var regex = GlobMatcher.MakeRe("src/**/*.js", options);

        Assert.Matches(regex, "src/components/Button.js");
        Assert.Matches(regex, "src/utils/format.js");
    }

    [Fact]
    public void DatabaseQuery_EscapeSpecialChars_InFilenames()
    {
        // Match files with special characters
        var regex = GlobMatcher.MakeRe("file\\(1\\).js");

        Assert.Matches(regex, "file(1).js");
    }

    [Fact]
    public void DatabaseQuery_LogFiles_ByExtension()
    {
        // Match log files: *.log, *.log.1, *.log.2, etc.
        var regex = GlobMatcher.MakeRe("*.log*");
        string pattern = regex.ToString();

        Assert.NotNull(pattern);

        Assert.Matches(regex, "app.log");
        Assert.Matches(regex, "app.log.1");
        Assert.Matches(regex, "error.log.gz");
    }

    [Fact]
    public void DatabaseQuery_SourceCode_ByDirectory()
    {
        // Match all source files in specific directories
        var regex = GlobMatcher.MakeRe("{src,lib}/**/*.{js,ts}");
        string pattern = regex.ToString();

        Assert.NotNull(pattern);

        Assert.Matches(regex, "src/app.js");
        Assert.Matches(regex, "lib/utils.ts");
        Assert.DoesNotMatch(regex, "test/app.test.js");
    }

    [Fact]
    public void DatabaseQuery_BaseName_IgnorePath()
    {
        // Match filename regardless of path
        var regex = GlobMatcher.MakeRe("README.md", new GlobbingOptions { BaseName = true });

        Assert.Matches(regex, "README.md");
    }

    [Fact]
    public void DatabaseQuery_HiddenFiles_WithDotOption()
    {
        // Match hidden files (starting with .)
        var regex = GlobMatcher.MakeRe(".*", new GlobbingOptions { Dot = true });
        string pattern = regex.ToString();

        Assert.NotNull(pattern);

        Assert.Matches(regex, ".gitignore");
        Assert.Matches(regex, ".env");
    }

    [Fact]
    public void DatabaseQuery_ExactMatch_NoWildcards()
    {
        // Exact filename match
        var regex = GlobMatcher.MakeRe("package.json");

        Assert.Matches(regex, "package.json");
        Assert.DoesNotMatch(regex, "package-lock.json");
    }

    [Fact]
    public void DatabaseQuery_BuildArtifacts_Exclusion()
    {
        // Exclude build artifacts - showing separate pattern checks
        string includePattern = "src/**/*.js";
        string excludeDist = "!**/dist/**";
        string excludeBuild = "!**/build/**";

        Assert.True(GlobMatcher.IsMatch("src/app.js", includePattern));

        // These should match the include pattern
        Assert.True(GlobMatcher.IsMatch("src/dist/app.js", includePattern));
        Assert.True(GlobMatcher.IsMatch("src/build/app.js", includePattern));

        // But be excluded by negation patterns
        Assert.False(GlobMatcher.IsMatch("src/dist/app.js", excludeDist));
        Assert.False(GlobMatcher.IsMatch("src/build/app.js", excludeBuild));
    }

    [Fact]
    public void DatabaseQuery_MediaFiles_GroupedByType()
    {
        // Match various media file types
        var regex = GlobMatcher.MakeRe("*.{jpg,jpeg,png,gif,svg,webp}");
        string pattern = regex.ToString();

        Assert.NotNull(pattern);

        Assert.Matches(regex, "photo.jpg");
        Assert.Matches(regex, "image.png");
        Assert.Matches(regex, "icon.svg");
        Assert.Matches(regex, "banner.webp");
        Assert.DoesNotMatch(regex, "video.mp4");
    }

    [Fact]
    public void DatabaseQuery_TemporaryFiles_Pattern()
    {
        // Match temporary files: *.tmp, *.temp, ~*
        string[] patterns = ["*.tmp", "*.temp", "~*"];

        Assert.True(GlobMatcher.IsMatch("file.tmp", patterns));
        Assert.True(GlobMatcher.IsMatch("data.temp", patterns));
        Assert.True(GlobMatcher.IsMatch("~backup.doc", patterns));
        Assert.False(GlobMatcher.IsMatch("document.pdf", patterns));
    }

    [Fact]
    public void DatabaseQuery_ConfigFiles_VariousFormats()
    {
        // Match config files: .config, config.*, *.config.js
        string[] patterns = [".config", "config.*", "*.config.{js,json,yaml,yml}"];

        Assert.True(GlobMatcher.IsMatch(".config", patterns));
        Assert.True(GlobMatcher.IsMatch("config.ini", patterns));
        Assert.True(GlobMatcher.IsMatch("webpack.config.js", patterns));
        Assert.True(GlobMatcher.IsMatch("app.config.json", patterns));
    }

    [Fact]
    public void DatabaseQuery_ArchiveFiles_Detection()
    {
        // Match archive files
        var regex = GlobMatcher.MakeRe("*.{zip,tar,gz,rar,7z,bz2}");

        Assert.Matches(regex, "backup.zip");
        Assert.Matches(regex, "archive.tar");
        Assert.Matches(regex, "data.gz");
        Assert.Matches(regex, "files.rar");
        Assert.DoesNotMatch(regex, "document.pdf");
    }

    [Fact]
    public void DatabaseQuery_PatternAsString_ForStorageAndReuse()
    {
        // Get the regex pattern as a string for database storage
        var regex = GlobMatcher.MakeRe("src/**/*.{js,ts}");
        string patternString = regex.ToString();
        var optionsValue = regex.Options;

        // Store in database as:
        // - pattern: patternString
        // - options: optionsValue (as enum/int)

        Assert.NotNull(patternString);
        Assert.NotEmpty(patternString);

        // Later retrieve and use:
        var reconstructed = new System.Text.RegularExpressions.Regex(patternString, optionsValue);
        Assert.Matches(reconstructed, "src/components/App.js");
    }
}
