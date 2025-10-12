# Documentation Content

**DEPRECATED**: This folder is no longer the primary source for documentation content.

## New System

As of the recent update, the documentation search system now indexes content directly from the `.razor` files in the `JwtIdentity.Client/Pages/Docs/` folder.

**You should now only update documentation in the .razor files.** The search index will automatically sync from those files daily at 2:00 AM via a Hangfire background job.

## Migration

The markdown files in this folder are kept for backward compatibility during the transition but are no longer actively maintained. They may be removed in a future update.

## Benefits

- **Single source of truth**: Update documentation in one place (the .razor files)
- **No duplication**: No need to maintain both .razor and .md files
- **Automatic sync**: Search index updates automatically from the live pages
