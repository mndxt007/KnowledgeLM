using KnowledgeLM.App.Data;
using KnowledgeLM.App.Models.Notes;
using KnowledgeLM.App.Models.Notes.DTOs;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeLM.App.Services;

/// <summary>
/// CRUD service for Notes entities using IDbContextFactory.
/// Each operation creates a short-lived DbContext to avoid concurrency issues in Blazor.
/// </summary>
public class NotesCRUDService
{
    private readonly IDbContextFactory<NotesContext> _contextFactory;

    public NotesCRUDService(IDbContextFactory<NotesContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // ─── Course Operations ───────────────────────────────────────

    public async Task<List<CourseResponse>> GetAllCoursesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Courses
            .AsNoTracking()
            .Include(c => c.Sections)
            .OrderByDescending(c => c.LastModified)
            .Select(c => new CourseResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Location = c.Location,
                CreatedAt = c.CreatedAt,
                LastModified = c.LastModified,
                SectionCount = c.Sections.Count
            })
            .ToListAsync();
    }

    public async Task<CourseDetailResponse?> GetCourseByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var course = await context.Courses
            .AsNoTracking()
            .Include(c => c.Sections)
                .ThenInclude(s => s.Resources)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course is null) return null;

        return new CourseDetailResponse
        {
            Id = course.Id,
            Name = course.Name,
            Description = course.Description,
            Location = course.Location,
            CreatedAt = course.CreatedAt,
            LastModified = course.LastModified,
            Sections = course.Sections.Select(s => new SectionDetailResponse
            {
                Id = s.Id,
                CourseId = s.CourseId,
                Name = s.Name,
                Description = s.Description,
                Location = s.Location,
                CreatedAt = s.CreatedAt,
                LastModified = s.LastModified,
                Resources = s.Resources.Select(r => new ResourceResponse
                {
                    Id = r.Id,
                    SectionId = r.SectionId,
                    Name = r.Name,
                    Location = r.Location,
                    ResourceType = r.ResourceType,
                    CreatedAt = r.CreatedAt,
                    LastModified = r.LastModified
                }).ToList()
            }).ToList()
        };
    }

    public async Task<CourseResponse> CreateCourseAsync(CreateCourseRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var course = new Course
        {
            Name = request.Name,
            Description = request.Description,
            Location = request.Location
        };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        return new CourseResponse
        {
            Id = course.Id,
            Name = course.Name,
            Description = course.Description,
            Location = course.Location,
            CreatedAt = course.CreatedAt,
            LastModified = course.LastModified,
            SectionCount = 0
        };
    }

    public async Task<bool> UpdateCourseAsync(UpdateCourseRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Courses.FindAsync(request.Id);
        if (existing is null) return false;

        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Location = request.Location;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCourseAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var course = await context.Courses.FindAsync(id);
        if (course is null) return false;

        context.Courses.Remove(course);
        await context.SaveChangesAsync();
        return true;
    }

    // ─── Section Operations ──────────────────────────────────────

    public async Task<List<SectionResponse>> GetSectionsByCourseIdAsync(int courseId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Sections
            .AsNoTracking()
            .Include(s => s.Resources)
            .Where(s => s.CourseId == courseId)
            .OrderBy(s => s.Name)
            .Select(s => new SectionResponse
            {
                Id = s.Id,
                CourseId = s.CourseId,
                Name = s.Name,
                Description = s.Description,
                Location = s.Location,
                CreatedAt = s.CreatedAt,
                LastModified = s.LastModified,
                ResourceCount = s.Resources.Count
            })
            .ToListAsync();
    }

    public async Task<SectionDetailResponse?> GetSectionByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var section = await context.Sections
            .AsNoTracking()
            .Include(s => s.Resources)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (section is null) return null;

        return new SectionDetailResponse
        {
            Id = section.Id,
            CourseId = section.CourseId,
            Name = section.Name,
            Description = section.Description,
            Location = section.Location,
            CreatedAt = section.CreatedAt,
            LastModified = section.LastModified,
            Resources = section.Resources.Select(r => new ResourceResponse
            {
                Id = r.Id,
                SectionId = r.SectionId,
                Name = r.Name,
                Location = r.Location,
                ResourceType = r.ResourceType,
                CreatedAt = r.CreatedAt,
                LastModified = r.LastModified
            }).ToList()
        };
    }

    public async Task<SectionResponse> CreateSectionAsync(CreateSectionRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var section = new Section
        {
            CourseId = request.CourseId,
            Name = request.Name,
            Description = request.Description,
            Location = request.Location
        };
        context.Sections.Add(section);
        await context.SaveChangesAsync();

        return new SectionResponse
        {
            Id = section.Id,
            CourseId = section.CourseId,
            Name = section.Name,
            Description = section.Description,
            Location = section.Location,
            CreatedAt = section.CreatedAt,
            LastModified = section.LastModified,
            ResourceCount = 0
        };
    }

    public async Task<bool> UpdateSectionAsync(UpdateSectionRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Sections.FindAsync(request.Id);
        if (existing is null) return false;

        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Location = request.Location;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteSectionAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var section = await context.Sections.FindAsync(id);
        if (section is null) return false;

        context.Sections.Remove(section);
        await context.SaveChangesAsync();
        return true;
    }

    // ─── Resource Operations ─────────────────────────────────────

    public async Task<List<ResourceResponse>> GetResourcesBySectionIdAsync(int sectionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Resources
            .AsNoTracking()
            .Where(r => r.SectionId == sectionId)
            .OrderBy(r => r.Name)
            .Select(r => new ResourceResponse
            {
                Id = r.Id,
                SectionId = r.SectionId,
                Name = r.Name,
                Location = r.Location,
                ResourceType = r.ResourceType,
                CreatedAt = r.CreatedAt,
                LastModified = r.LastModified
            })
            .ToListAsync();
    }

    public async Task<ResourceResponse?> GetResourceByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var resource = await context.Resources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (resource is null) return null;

        return new ResourceResponse
        {
            Id = resource.Id,
            SectionId = resource.SectionId,
            Name = resource.Name,
            Location = resource.Location,
            ResourceType = resource.ResourceType,
            CreatedAt = resource.CreatedAt,
            LastModified = resource.LastModified
        };
    }

    public async Task<ResourceResponse> CreateResourceAsync(CreateResourceRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var resource = new Resource
        {
            SectionId = request.SectionId,
            Name = request.Name,
            Location = request.Location,
            ResourceType = request.ResourceType
        };
        context.Resources.Add(resource);
        await context.SaveChangesAsync();

        return new ResourceResponse
        {
            Id = resource.Id,
            SectionId = resource.SectionId,
            Name = resource.Name,
            Location = resource.Location,
            ResourceType = resource.ResourceType,
            CreatedAt = resource.CreatedAt,
            LastModified = resource.LastModified
        };
    }

    public async Task<bool> UpdateResourceAsync(UpdateResourceRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Resources.FindAsync(request.Id);
        if (existing is null) return false;

        existing.Name = request.Name;
        existing.Location = request.Location;
        existing.ResourceType = request.ResourceType;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteResourceAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var resource = await context.Resources.FindAsync(id);
        if (resource is null) return false;

        context.Resources.Remove(resource);
        await context.SaveChangesAsync();
        return true;
    }
}
