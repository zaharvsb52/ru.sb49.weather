namespace Sb49.Weather.Droid.Model
{
    public sealed class MenuItem
    {
        public MenuItem(int id, int? menuId = null, MenuItemTypes menuItemType = MenuItemTypes.Default)
        {
            Id = id;
            MenuId = menuId ?? int.MinValue;
            MenuItemType = menuItemType;
            IsReadOnly = true;
        }

        public int Id { get; set; }
        public int MenuId { get; }
        public int? KeyId { get; set; }
        public MenuItemTypes MenuItemType { get; }
        public int? TitleId { get; set; }
        public string Title { get; set; }
        public int? IconId { get; set; }
        public bool IsReadOnly { get; set; }
        public int? AttrTintColorId { get; set; }
        public bool IsRestoredDefaultTintMode { get; set; }
    }
}