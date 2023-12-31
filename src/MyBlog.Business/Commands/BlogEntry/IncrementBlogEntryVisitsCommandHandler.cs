﻿using Microsoft.EntityFrameworkCore;
using MyBlog.Data;

namespace MyBlog.Business.Commands;

public class IncrementBlogEntryVisitsCommandHandler :
    ICommandHandler<IncrementBlogEntryVisitsCommand>
{
    private readonly EFUnitOfWork unitOfWork;

    public IncrementBlogEntryVisitsCommandHandler(EFUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(IncrementBlogEntryVisitsCommand command)
    {
        await this.unitOfWork.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [BlogEntries] SET [Visits] = [Visits] + 1 WHERE [Id] = {command.Id}");
    }
}