namespace NewsPortalPro.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? ColorCode { get; set; }
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public bool ShowInMenu { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public List<CategoryDto> Children { get; set; } = [];
    }

    public class CategoryWithCountDto : CategoryDto
    {
        public int NewsCount { get; set; }
    }

    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ColorCode { get; set; }
        public int? ParentId { get; set; }
        public int DisplayOrder { get; set; }
        public bool ShowInMenu { get; set; } = true;
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
    }

    public class UpdateCategoryDto : CreateCategoryDto
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
    }
}