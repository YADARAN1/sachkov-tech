using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using SachkovTech.Application.IssueManagement;
using SachkovTech.Domain.IssueManagement;
using SachkovTech.Domain.IssueManagement.Entities;
using SachkovTech.Domain.Shared;
using SachkovTech.Domain.Shared.ValueObjects;
using SachkovTech.Domain.Shared.ValueObjects.Ids;
using SachkovTech.Infrastructure.DbContexts;

namespace SachkovTech.Infrastructure.Repositories;

public class ModulesRepository : IModulesRepository
{
    private readonly WriteDbContext _dbContext;

    public ModulesRepository(WriteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Add(Module module, CancellationToken cancellationToken = default)
    {
        await _dbContext.Modules.AddAsync(module, cancellationToken);
        return module.Id;
    }

    public Guid Save(Module module, CancellationToken cancellationToken = default)
    {
        _dbContext.Modules.Attach(module);
        return module.Id.Value;
    }

    public Guid Delete(Module module, CancellationToken cancellationToken = default)
    {
        _dbContext.Modules.Remove(module);
        return module.Id;
    }

    public async Task<Result<Guid, ErrorList>> DeleteIssue(ModuleId moduleId, IssueId issueId,
        CancellationToken cancellationToken = default)
    {
        var module = await _dbContext.Modules
            .Include(m => m.Issues)
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken);

        if (module == null)
            return Errors.General.NotFound(moduleId).ToErrorList();
        
        var issue = module.Issues.FirstOrDefault(m => m.Id == issueId);
        if (issue == null)
            return Errors.General.NotFound(issueId).ToErrorList();
            
        var result = module.DeleteIssue(issueId);
        if (result.IsFailure)
            return result.Error.ToErrorList();

        _dbContext.Set<Issue>().Remove(issue);
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        return issueId.Value;
    }

    public async Task<Result<Module, Error>> GetById(
        ModuleId moduleId, CancellationToken cancellationToken = default)
    {
        var module = await _dbContext.Modules
            .Include(m => m.Issues)
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken);

        if (module is null)
            return Errors.General.NotFound(moduleId);

        return module;
    }

    public async Task<Result<Module, Error>> GetByTitle(
        Title title, CancellationToken cancellationToken = default)
    {
        var module = await _dbContext.Modules
            .Include(m => m.Issues)
            .FirstOrDefaultAsync(m => m.Title == title, cancellationToken);

        if (module is null)
            return Errors.General.NotFound();

        return module;
    }
}