namespace Snowberry.Globbing.Tests;

/// <summary>
/// Real-world scenario tests demonstrating practical usage patterns
/// </summary>
public class RealWorldScenariosTests
{
    [Fact]
    public void Scenario_GitignorePatterns_MatchCorrectly()
    {
        // Common .gitignore patterns
        string[] patterns =
        [
            "node_modules/**",
            "*.log",
            "dist/**",
            ".env",
            "coverage/**"
        ];

        // Should match
        Assert.True(GlobMatcher.IsMatch("node_modules/package/index.js", patterns));
        Assert.True(GlobMatcher.IsMatch("error.log", patterns));
        Assert.True(GlobMatcher.IsMatch("dist/bundle.js", patterns));
        Assert.True(GlobMatcher.IsMatch(".env", patterns));
        Assert.True(GlobMatcher.IsMatch("coverage/index.html", patterns));

        // Should not match
        Assert.False(GlobMatcher.IsMatch("src/app.js", patterns));
        Assert.False(GlobMatcher.IsMatch("README.md", patterns));
    }

    [Fact]
    public void Scenario_TypeScriptProject_FileSelection()
    {
        // Select TypeScript source files but not tests or definitions
        string includePattern = "**/*.ts";
        string excludeTests = "!**/*.test.ts";
        string excludeSpecs = "!**/*.spec.ts";
        string excludeDefs = "!**/*.d.ts";

        // Source files should match
        Assert.True(GlobMatcher.IsMatch("src/app.ts", includePattern));
        Assert.True(GlobMatcher.IsMatch("src/services/api.ts", includePattern));

        // Test files should be excluded
        Assert.False(GlobMatcher.IsMatch("src/app.test.ts", excludeTests));
        Assert.False(GlobMatcher.IsMatch("src/utils.spec.ts", excludeSpecs));
        Assert.False(GlobMatcher.IsMatch("types/index.d.ts", excludeDefs));
    }

    [Fact]
    public void Scenario_WebpackAssetMatching_Images()
    {
        // Match common image formats
        string imagePattern = "**/*.{jpg,jpeg,png,gif,svg,webp,ico}";
        var matcher = GlobMatcher.Create(imagePattern);

        Assert.True(matcher("assets/logo.png"));
        Assert.True(matcher("public/images/hero.jpg"));
        Assert.True(matcher("src/icons/menu.svg"));
        Assert.True(matcher("favicon.ico"));
        Assert.False(matcher("styles/main.css"));
        Assert.False(matcher("scripts/app.js"));
    }

    [Fact]
    public void Scenario_BuildTool_SourceAndTestSeparation()
    {
        // Build tool needs to distinguish between source and test files
        string sourcePattern = "src/**/*.{js,ts,tsx}";
        string testPattern = "{test,tests,__tests__}/**/*.{js,ts}";

        Assert.True(GlobMatcher.IsMatch("src/components/Button.tsx", sourcePattern));
        Assert.True(GlobMatcher.IsMatch("test/unit/Button.test.js", testPattern));
        Assert.True(GlobMatcher.IsMatch("__tests__/integration/api.spec.ts", testPattern));
        Assert.False(GlobMatcher.IsMatch("test/unit/Button.test.js", sourcePattern));
    }

    [Fact]
    public void Scenario_MonorepoPackageSelection()
    {
        // Select files from specific packages in a monorepo
        string pattern = "packages/{core,utils,components}/src/**/*.ts";
        var matcher = GlobMatcher.Create(pattern);

        Assert.True(matcher("packages/core/src/index.ts"));
        Assert.True(matcher("packages/utils/src/helpers.ts"));
        Assert.True(matcher("packages/components/src/Button.ts"));
        Assert.False(matcher("packages/docs/src/index.ts"));
        Assert.False(matcher("packages/core/test/index.test.ts"));
    }

    [Fact]
    public void Scenario_LogFileRotation_Matching()
    {
        // Match log files including rotated ones
        string logPattern = "*.log*";
        var matcher = GlobMatcher.Create(logPattern);

        Assert.True(matcher("app.log"));
        Assert.True(matcher("error.log.1"));
        Assert.True(matcher("access.log.2024-01-01"));
        Assert.True(matcher("debug.log.gz"));
        Assert.False(matcher("readme.txt"));
    }

    [Fact]
    public void Scenario_BackupFiles_Detection()
    {
        // Detect various backup file patterns
        string[] backupPatterns =
        [
            "*.bak",
            "*.backup",
            "*~",
            "*.old",
            "#*#"
        ];

        Assert.True(GlobMatcher.IsMatch("config.bak", backupPatterns));
        Assert.True(GlobMatcher.IsMatch("data.backup", backupPatterns));
        Assert.True(GlobMatcher.IsMatch("file.txt~", backupPatterns));
        Assert.True(GlobMatcher.IsMatch("settings.old", backupPatterns));
        Assert.True(GlobMatcher.IsMatch("#auto-save#", backupPatterns));
        Assert.False(GlobMatcher.IsMatch("document.pdf", backupPatterns));
    }

    [Fact]
    public void Scenario_MediaLibrary_FileTypeGrouping()
    {
        // Organize media files by type
        string videoPattern = "**/*.{mp4,avi,mov,wmv,flv,mkv}";
        string audioPattern = "**/*.{mp3,wav,flac,aac,ogg,m4a}";
        string imagePattern = "**/*.{jpg,png,gif,bmp,svg,webp}";

        var videoMatcher = GlobMatcher.Create(videoPattern);
        var audioMatcher = GlobMatcher.Create(audioPattern);
        var imageMatcher = GlobMatcher.Create(imagePattern);

        Assert.True(videoMatcher("media/videos/presentation.mp4"));
        Assert.True(audioMatcher("music/song.mp3"));
        Assert.True(imageMatcher("photos/vacation.jpg"));

        Assert.False(videoMatcher("music/song.mp3"));
        Assert.False(audioMatcher("photos/vacation.jpg"));
        Assert.False(imageMatcher("media/videos/presentation.mp4"));
    }

    [Fact]
    public void Scenario_DocumentFormats_Filtering()
    {
        // Filter documents by format
        string officePattern = "**/*.{doc,docx,xls,xlsx,ppt,pptx}";

        var officeMatcher = GlobMatcher.Create(officePattern);

        Assert.True(officeMatcher("reports/Q1.xlsx"));
        Assert.True(officeMatcher("presentations/meeting.pptx"));
        Assert.True(officeMatcher("documents/contract.docx"));
        Assert.False(officeMatcher("README.md"));
    }

    [Fact]
    public void Scenario_VersionControl_IgnorePatterns()
    {
        // Common version control ignore patterns
        string[] ignorePatterns =
        [
            ".git/**",
            ".svn/**",
            ".hg/**",
            "*.swp",
            "*.swo",
            ".DS_Store",
            "Thumbs.db"
        ];

        Assert.True(GlobMatcher.IsMatch(".git/config", ignorePatterns));
        Assert.True(GlobMatcher.IsMatch("file.swp", ignorePatterns));
        Assert.True(GlobMatcher.IsMatch(".DS_Store", ignorePatterns));
        Assert.True(GlobMatcher.IsMatch("Thumbs.db", ignorePatterns));
    }

    [Fact]
    public void Scenario_ConfigFiles_MultipleLocations()
    {
        // Find config files in various standard locations
        string[] configPatterns =
        [
            ".*.{json,yml,yaml,toml}",
            "*.config.{js,ts,json}",
            "config/**/*.{json,yml,yaml}"
        ];

        Assert.True(GlobMatcher.IsMatch(".eslintrc.json", configPatterns));
        Assert.True(GlobMatcher.IsMatch("webpack.config.js", configPatterns));
        Assert.True(GlobMatcher.IsMatch("config/database.yml", configPatterns));
        Assert.True(GlobMatcher.IsMatch(".prettierrc.yaml", configPatterns));
    }

    [Fact]
    public void Scenario_TemporaryFiles_Cleanup()
    {
        // Match temporary files for cleanup
        string[] tempPatterns =
        [
            "tmp/**",
            "temp/**",
            "*.tmp",
            "*.temp",
            "*.cache"
        ];

        Assert.True(GlobMatcher.IsMatch("tmp/upload-123.dat", tempPatterns));
        Assert.True(GlobMatcher.IsMatch("temp/session-data", tempPatterns));
        Assert.True(GlobMatcher.IsMatch("build.tmp", tempPatterns));
        Assert.True(GlobMatcher.IsMatch("data.cache", tempPatterns));
    }

    [Fact]
    public void Scenario_SourceCodeLinting_FileSelection()
    {
        // ESLint/TSLint file selection
        string lintableFiles = "**/*.{js,jsx,ts,tsx}";
        string[] excludePatterns = ["!node_modules/**", "!dist/**", "!build/**", "!coverage/**"];

        var matcher = GlobMatcher.Create(lintableFiles);

        Assert.True(matcher("src/App.tsx"));
        Assert.True(matcher("components/Button.jsx"));
        Assert.True(matcher("utils/helpers.ts"));

        // These would be excluded by the exclude patterns in a real scenario
        Assert.True(matcher("node_modules/lib/index.js")); // Matches pattern but would be excluded
    }

    [Fact]
    public void Scenario_DatabaseBackup_FileNaming()
    {
        // Match database backup files with timestamps
        string backupPattern = "backup-*.{sql,db,dump}";
        string timestampPattern = "backup-[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9].sql";

        var matcher = GlobMatcher.Create(backupPattern);
        var timestampMatcher = GlobMatcher.Create(timestampPattern);

        Assert.True(matcher("backup-20240101.sql"));
        Assert.True(matcher("backup-latest.db"));
        Assert.True(timestampMatcher("backup-2024-01-15.sql"));
        Assert.False(timestampMatcher("backup-latest.sql"));
    }

    [Fact]
    public void Scenario_APIVersioning_RouteMatching()
    {
        // Match API versioned endpoints
        string anyVersionPattern = "api/v[0-9]/**";

        var anyMatcher = GlobMatcher.Create(anyVersionPattern);

        Assert.True(anyMatcher("api/v1/users"));
        Assert.True(anyMatcher("api/v2/products"));
        Assert.True(anyMatcher("api/v3/orders"));
        Assert.False(anyMatcher("api/users"));
    }

    [Fact]
    public void Scenario_FrontendAssets_Organization()
    {
        // Organize frontend assets by type and environment
        string styles = "**/*.{css,scss,sass,less}";
        string fonts = "**/*.{woff,woff2,ttf,eot,otf}";

        var stylesMatcher = GlobMatcher.Create(styles);
        var fontsMatcher = GlobMatcher.Create(fonts);

        Assert.True(stylesMatcher("src/styles/main.scss"));
        Assert.True(stylesMatcher("dist/css/app.min.css"));
        Assert.True(fontsMatcher("assets/fonts/roboto.woff2"));
        Assert.True(fontsMatcher("public/fonts/icons.ttf"));
    }

    [Fact]
    public void Scenario_TestFramework_FileDiscovery()
    {
        // Jest/Mocha test file discovery
        string[] testPatterns =
        [
            "**/*.test.{js,ts}",
            "**/*.spec.{js,ts}",
            "**/__tests__/**/*.{js,ts}"
        ];

        Assert.True(GlobMatcher.IsMatch("src/utils/format.test.js", testPatterns));
        Assert.True(GlobMatcher.IsMatch("components/Button.spec.ts", testPatterns));
        Assert.True(GlobMatcher.IsMatch("src/__tests__/integration/api.js", testPatterns));
        Assert.False(GlobMatcher.IsMatch("src/app.js", testPatterns));
    }

    [Fact]
    public void Scenario_DockerIgnore_Patterns()
    {
        // Common .dockerignore patterns
        string[] dockerIgnorePatterns =
        [
            "node_modules",
            "npm-debug.log",
            "Dockerfile*",
            "docker-compose*",
            ".git",
            ".gitignore",
            "README.md",
            ".env*",
            "*.md"
        ];

        Assert.True(GlobMatcher.IsMatch("node_modules", dockerIgnorePatterns));
        Assert.True(GlobMatcher.IsMatch("Dockerfile.dev", dockerIgnorePatterns));
        Assert.True(GlobMatcher.IsMatch(".env.local", dockerIgnorePatterns));
        Assert.True(GlobMatcher.IsMatch("CHANGELOG.md", dockerIgnorePatterns));
    }

    [Fact]
    public void Scenario_ContinuousIntegration_ArtifactCollection()
    {
        // CI/CD artifact collection patterns
        string[] artifactPatterns =
        [
            "dist/**/*.{js,css,html}",
            "build/**/*.{exe,dll,so}",
            "target/**/*.jar",
            "*.{zip,tar,gz,tgz}"
        ];

        Assert.True(GlobMatcher.IsMatch("dist/bundle.min.js", artifactPatterns));
        Assert.True(GlobMatcher.IsMatch("build/app.exe", artifactPatterns));
        Assert.True(GlobMatcher.IsMatch("target/myapp-1.0.jar", artifactPatterns));
        Assert.True(GlobMatcher.IsMatch("release-v1.0.0.zip", artifactPatterns));
    }

    [Fact]
    public void Scenario_SecurityScan_SensitiveFiles()
    {
        // Detect potentially sensitive files
        string[] sensitivePatterns =
        [
            "**/*.{key,pem,p12,pfx}",
            ".env*",
            "**/.env*",
            "*credentials*",
            "**/credentials*",
            "*secrets*",
            "**/secrets*",
            "*password*",
            "**/*password*",
            "*secret*",
            "**/*secret*"
        ];

        Assert.True(GlobMatcher.IsMatch("config/server.key", sensitivePatterns));
        Assert.True(GlobMatcher.IsMatch(".env.production", sensitivePatterns));
        Assert.True(GlobMatcher.IsMatch("aws-credentials.json", sensitivePatterns));
        Assert.True(GlobMatcher.IsMatch("database-password.txt", sensitivePatterns));
        Assert.True(GlobMatcher.IsMatch("config/my-secret.conf", sensitivePatterns));
    }
}
