﻿using System.Data;

namespace MyBlog.Data.Validation;

[Serializable]
public class ValidationException : DataException
{
    public ValidationException()
    {
        this.ValidationResults = new List<EntityValidationResult>();
    }

    public ValidationException(IEnumerable<EntityValidationResult> validationResults)
    {
        this.ValidationResults = validationResults;
    }

    public IEnumerable<EntityValidationResult> ValidationResults { get; }
}