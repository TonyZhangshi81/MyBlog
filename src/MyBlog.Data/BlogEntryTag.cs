﻿using System.ComponentModel.DataAnnotations;
using MyBlog.Localization;

namespace MyBlog.Data;

public class BlogEntryTag
{
    [Required(ErrorMessageResourceName = nameof(Resources.Validation_Required), ErrorMessageResourceType = typeof(Resources))]
    public Guid? BlogEntryId { get; set; }

    public virtual BlogEntry? BlogEntry { get; set; }

    [Required(ErrorMessageResourceName = nameof(Resources.Validation_Required), ErrorMessageResourceType = typeof(Resources))]
    public Guid? TagId { get; set; }

    public virtual Tag? Tag { get; set; }
}