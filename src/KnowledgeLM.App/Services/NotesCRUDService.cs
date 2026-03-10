using System.ComponentModel;
using KnowledgeLM.App.Data;
using KnowledgeLM.App.Models.Notes;
using KnowledgeLM.App.Models.Notes.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Resource = KnowledgeLM.App.Models.Notes.Resource;

namespace KnowledgeLM.App.Services;

/// <summary>
/// CRUD service for Notes entities using IDbContextFactory.
/// Each operation creates a short-lived DbContext to avoid concurrency issues in Blazor.
/// </summary>

public class NotesCRUDService
{
    private readonly IDbContextFactory<NotesContext> _contextFactory;
    private readonly StateService _stateService;

    public NotesCRUDService(IDbContextFactory<NotesContext> contextFactory, StateService stateService)
    {
        _contextFactory = contextFactory;
        _stateService = stateService;
    }

    private async Task SaveAndNotifyAsync(NotesContext context)
    {
        await context.SaveChangesAsync();
        _stateService.RequestRefresh();
    }

    // ─── Course Operations ───────────────────────────────────────

    [KernelFunction("get_all_courses")]
    [Description("Gets all courses ordered by last modified date, including section counts.")]
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

    [KernelFunction("get_course_by_id")]
    [Description("Gets a course by its ID, including its sections and their resources.")]
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

    [KernelFunction("create_course")]
    [Description("Creates a new course with the specified name, description, and location.")]
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
        await SaveAndNotifyAsync(context);

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

    [KernelFunction("update_course")]
    [Description("Updates an existing course's name, description, and location.")]
    public async Task<bool> UpdateCourseAsync(UpdateCourseRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Courses.FindAsync(request.Id);
        if (existing is null) return false;

        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Location = request.Location;

        await SaveAndNotifyAsync(context);
        return true;
    }

    public async Task<bool> DeleteCourseAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var course = await context.Courses.FindAsync(id);
        if (course is null) return false;

        context.Courses.Remove(course);
        await SaveAndNotifyAsync(context);
        return true;
    }

    // ─── Section Operations ──────────────────────────────────────

    [KernelFunction("get_sections_by_course_id")]
    [Description("Gets all sections for a given course ID, including resource counts.")]
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

    [KernelFunction("get_section_by_id")]
    [Description("Gets a section by its ID, including its resources.")]
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

    [KernelFunction("create_section")]
    [Description("Creates a new section with the specified course ID, name, description, and location.")]
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
        await SaveAndNotifyAsync(context);

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

    [KernelFunction("update_section")]
    [Description("Updates an existing section's name, description, and location.")]
    public async Task<bool> UpdateSectionAsync(UpdateSectionRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Sections.FindAsync(request.Id);
        if (existing is null) return false;

        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Location = request.Location;

        await SaveAndNotifyAsync(context);
        return true;
    }

    public async Task<bool> DeleteSectionAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var section = await context.Sections.FindAsync(id);
        if (section is null) return false;

        context.Sections.Remove(section);
        await SaveAndNotifyAsync(context);
        return true;
    }

    // ─── Resource Operations ─────────────────────────────────────

    [KernelFunction("get_resources_by_section_id")]
    [Description("Gets all resources for a given section ID.")]
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

    [KernelFunction("get_resource_by_id")]
    [Description("Gets a resource by its ID.")]
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

    [KernelFunction("create_resource")]
    [Description("Creates a new resource with the specified section ID, name, location, and resource type.")]
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
        await SaveAndNotifyAsync(context);

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

    [KernelFunction("update_resource")]
    [Description("Updates an existing resource's name, location, and resource type.")]
    public async Task<bool> UpdateResourceAsync(UpdateResourceRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Resources.FindAsync(request.Id);
        if (existing is null) return false;

        existing.Name = request.Name;
        existing.Location = request.Location;
        existing.ResourceType = request.ResourceType;

        await SaveAndNotifyAsync(context);
        return true;
    }

    public async Task<bool> DeleteResourceAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var resource = await context.Resources.FindAsync(id);
        if (resource is null) return false;

        context.Resources.Remove(resource);
        await SaveAndNotifyAsync(context);
        return true;
    }

    // ─── Breadcrumb Operations ───────────────────────────────────

    [KernelFunction("get_breadcrumb")]
    [Description("Gets the breadcrumb path (Course > Section > Resource) for a given resource ID.")]
    public async Task<ResourceBreadcrumbDto?> GetBreadcrumbAsync(int resourceId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var resource = await context.Resources
            .AsNoTracking()
            .Include(r => r.Section)
                .ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(r => r.Id == resourceId);

        if (resource is null) return null;

        return new ResourceBreadcrumbDto
        {
            CourseId = resource.Section.Course.Id,
            CourseName = resource.Section.Course.Name,
            SectionId = resource.Section.Id,
            SectionName = resource.Section.Name,
            ResourceId = resource.Id,
            ResourceName = resource.Name
        };
    }
}
