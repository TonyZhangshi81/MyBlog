﻿using MyBlog.Data;

namespace MyBlog.Web.Models.Blog;

public class EntryViewModel
{
    public BlogEntry BlogEntry { get; set; } = null!;

    public List<BlogEntry> RelatedBlogEntries { get; set; } = null!;

    public BlogEntryCommentViewModel Comment { get; set; } = null!;
}
