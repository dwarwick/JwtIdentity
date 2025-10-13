# Documentation Content

**DEPRECATED**: This folder is no longer the primary source for documentation content.

## New System

As of the recent update, the documentation search system now indexes content directly from the `.razor` files in the `JwtIdentity.Client/Pages/Docs/` folder.

**You should now only update documentation in the .razor files.** The `.razor` files are automatically copied to `DocsContent/RazorPages/` during the build process, and the search index syncs from these copied files daily at 2:00 AM via a Hangfire background job.

## How It Works

1. **During Build**: The `.razor` files from `JwtIdentity.Client/Pages/Docs/` are copied to `JwtIdentity/DocsContent/RazorPages/` 
2. **During Deployment**: These copied files are included in the deployment package
3. **At Runtime**: The `DocsSearchIndexer` reads from the deployed `.razor` files to build the search index
4. **Daily Sync**: A Hangfire job runs at 2:00 AM to keep the search index up-to-date

This ensures the `.razor` files are available even when deployed, as they're part of the server project's output.

**Note**: The `RazorPages/` folder is in `.gitignore` because these files are generated during build and should not be committed to source control. The source of truth is the `.razor` files in the `JwtIdentity.Client/Pages/Docs/` folder.

## Migration

The markdown files in this folder are kept for backward compatibility during the transition but are no longer actively maintained. They may be removed in a future update.

## Benefits

- **Single source of truth**: Update documentation in one place (the .razor files)
- **No duplication**: No need to maintain both .razor and .md files
- **Automatic sync**: Search index updates automatically from the live pages
- **Works in deployment**: Build process ensures files are available at runtime
