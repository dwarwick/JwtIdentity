using Microsoft.AspNetCore.Components.Web;

namespace JwtIdentity.Client.Pages.Common
{
    public class DragAndDropModel<TItem> : BlazorBase
    {
        [Parameter]
        public List<TItem> Items { get; set; }

        [Parameter]
        public TItem Item { get; set; }

        [Parameter]
        public EventCallback<List<TItem>> ItemsChanged { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        protected int DragIndex { get; set; } = -1;

        protected int DragOverIndex { get; set; } = -1;
        protected async Task OnDragStart(DragEventArgs e, int index)
        {
            if (Disabled) return;
            DragOverIndex = -1;

            if (index == -1)
            {
                DragIndex = -1;

                return;
            }

            DragIndex = index;

            await LocalStorage.SetItemAsync("DragIndex", index);
        }

        protected async Task OnDragOver(DragEventArgs e, int index)
        {
            if (Disabled) return;
            DragIndex = await LocalStorage.GetItemAsync<int>("DragIndex");

            if (DragIndex != -1 && index != -1)
            {
                DragOverIndex = index;
            }
            else
            {
                DragIndex = -1;
                DragOverIndex = -1;
            }
        }

        protected void OnDragLeave(DragEventArgs e)
        {
            if (Disabled) return;
            // Reset the drag-over index so previous lines disappear
            DragOverIndex = -1;
        }

        protected async Task OnDrop(DragEventArgs e, int index)
        {
            if (Disabled) return;
            DragOverIndex = -1;

            DragIndex = await LocalStorage.GetItemAsync<int>("DragIndex");
            if (DragIndex != -1 && index != -1)
            {
                var item = Items[DragIndex];

                Items.RemoveAt(DragIndex);
                Items.Insert(index, item);

                DragIndex = -1;
                DragOverIndex = -1;

                await ItemsChanged.InvokeAsync(Items);
            }
            else
            {
                DragIndex = -1;
                DragOverIndex = -1;
            }
        }
    }
}
