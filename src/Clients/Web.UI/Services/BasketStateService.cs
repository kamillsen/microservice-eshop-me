namespace Web.UI.Services;

public class BasketStateService
{
    private int _itemCount = 0;

    public int ItemCount
    {
        get => _itemCount;
        private set
        {
            if (_itemCount != value)
            {
                _itemCount = value;
                OnItemCountChanged?.Invoke();
            }
        }
    }

    public event Action? OnItemCountChanged;

    public void UpdateItemCount(int count)
    {
        ItemCount = count;
    }

    public void Clear()
    {
        ItemCount = 0;
    }
}

