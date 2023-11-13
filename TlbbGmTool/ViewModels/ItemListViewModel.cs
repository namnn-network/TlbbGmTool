using liuguang.TlbbGmTool.Common;
using liuguang.TlbbGmTool.Models;
using liuguang.TlbbGmTool.Services;
using liuguang.TlbbGmTool.ViewModels.Data;
using liuguang.TlbbGmTool.Views.Item;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace liuguang.TlbbGmTool.ViewModels;

public class ItemListViewModel : ViewModelBase
{
    #region Fields
    /// <summary>
    /// 数据库连接
    /// </summary>
    public DbConnection? Connection;
    #endregion

    #region Properties

    public BagContainer ItemsContainer { get; } = new();

    public Visibility AddEquipVisible => ItemsContainer.RoleBagType == BagType.ItemBag ? Visibility.Visible : Visibility.Collapsed;
    public Visibility AddGemVisible => ItemsContainer.RoleBagType == BagType.MaterialBag ? Visibility.Visible : Visibility.Collapsed;
    private bool CanInsertItem => ItemsContainer.ItemList.Count < ItemsContainer.BagMaxSize;

    /// <summary>
    /// 弹出物品编辑窗体
    /// </summary>
    public Command EditItemCommand { get; }
    /// <summary>
    /// 复制物品命令
    /// </summary>
    public Command CopyItemCommand { get; }
    /// <summary>
    /// 删除物品命令
    /// </summary>
    public Command DeleteItemCommand { get; }
    /// <summary>
    /// 显示发放装备窗体
    /// </summary>
    public Command AddEquipCommand { get; }
    /// <summary>
    /// 显示发放宝石窗体
    /// </summary>
    public Command AddGemCommand { get; }
    /// <summary>
    /// 显示发放道具窗体
    /// </summary>
    public Command AddItemCommand { get; }
    #endregion

    public ItemListViewModel()
    {
        EditItemCommand = new(ShowItemEditor);
        CopyItemCommand = new(ProcessCopyItem);
        DeleteItemCommand = new(ProcessDeleteItem);
        AddEquipCommand = new(ShowAddEquipEditor, () => CanInsertItem);
        AddGemCommand = new(ShowAddGemEditor, () => CanInsertItem);
        AddItemCommand = new(ShowAddItemEditor, () => CanInsertItem);
        ItemsContainer.PropertyChanged += ItemsContainer_PropertyChanged;
        ItemsContainer.ItemList.CollectionChanged += ItemList_CollectionChanged;
    }

    /// <summary>
    /// 当物品列表的长度变化时，更新发放按钮的状态(包满了就不允许发放了)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void ItemList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        AddEquipCommand.RaiseCanExecuteChanged();
        AddGemCommand.RaiseCanExecuteChanged();
        AddItemCommand.RaiseCanExecuteChanged();
    }

    private void ItemsContainer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ItemsContainer.RoleBagType))
        {
            RaisePropertyChanged(nameof(AddEquipVisible));
            RaisePropertyChanged(nameof(AddGemVisible));
        }
    }

    public async Task LoadItemListAsync()
    {
        if (Connection is null)
        {
            return;
        }
        try
        {
            var itemList = await Task.Run(async () =>
            {
                return await ItemDbService.LoadItemListAsync(Connection, ItemsContainer.CharGuid, ItemsContainer.PosOffset, ItemsContainer.BagMaxSize);
            });
            ItemsContainer.FillItemList(itemList);
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Lỗi tải dữ liệu!", ex);
        }
    }

    private void ShowItemEditor(object? parameter)
    {
        if (parameter is not ItemLogViewModel itemLog)
        {
            return;
        }
        if (itemLog.ItemClass == 1)
        {
            ShowDialog(new EquipEditorWindow(), (EquipEditorViewModel vm) =>
            {
                vm.ItemLog = itemLog;
                vm.Connection = Connection;
            });
        }
        else if ((itemLog.ItemClass >= 2) && (itemLog.ItemClass <= 4))
        {
            ShowDialog(new CommonItemEditorWindow(), (CommonItemEditorViewModel vm) =>
            {
                vm.ItemLog = itemLog;
                vm.RoleBagType = ItemsContainer.RoleBagType;
                vm.Connection = Connection;
            });
        }
        else if (itemLog.ItemClass == 5)
        {
            ShowDialog(new GemEditorWindow(), (GemEditorViewModel vm) =>
            {
                vm.ItemLog = itemLog;
                vm.Connection = Connection;
            });
        }
        else
        {
            ShowErrorMessage("Lỗi", $"Loại hình không xác định class={itemLog.ItemClass}");
        }
    }
    private async void ProcessCopyItem(object? parameter)
    {
        if (Connection is null)
        {
            return;
        }
        if (parameter is not ItemLogViewModel itemLog)
        {
            return;
        }
        if (!Confirm("Xác nhận", $"Có chắc chắn muốn sao chép vật phẩm{itemLog.ItemName} ?"))
        {
            return;
        }
        var pData = new byte[itemLog.PData.Length];
        Array.Copy(itemLog.PData, pData, pData.Length);
        ItemLogViewModel newItemLog = new(new()
        {
            CharGuid = itemLog.CharGuid,
            ItemBaseId = itemLog.ItemBaseId,
            PData = pData,
            Creator = itemLog.Creator,
        });
        try
        {
            await Task.Run(async () =>
            {
                await ItemDbService.InsertItemAsync(Connection, ItemsContainer.PosOffset, ItemsContainer.BagMaxSize, newItemLog);
            });
            ItemsContainer.InsertNewItem(newItemLog);
            ShowMessage("Đã sao chép thành công", $"Thành công sao chép vật phẩm {newItemLog.ItemName},pos={newItemLog.Pos}");
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Sao chép không thành công!", ex, true);
        }
    }

    private async void ProcessDeleteItem(object? parameter)
    {
        if (Connection is null)
        {
            return;
        }
        if (parameter is not ItemLogViewModel itemLog)
        {
            return;
        }
        if (!Confirm("Xác nhận", $"Có chắc chắn muốn xoá vật phẩm {itemLog.ItemName}?"))
        {
            return;
        }
        try
        {
            await Task.Run(async () =>
            {
                await ItemDbService.DeleteItemAsync(Connection, itemLog.Id);
            });
            ItemsContainer.ItemList.Remove(itemLog);
            ShowMessage("Xoá thành công", $"Xoá thành công vật phẩm {itemLog.ItemName}!");
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Xoá không thành công!", ex, true);
        }

    }
    private void ShowAddEquipEditor()
    {
        ShowDialog(new EquipEditorWindow(), (EquipEditorViewModel vm) =>
        {
            vm.Connection = Connection;
            vm.ItemsContainer = ItemsContainer;
        });
    }
    private void ShowAddGemEditor()
    {
        ShowDialog(new GemEditorWindow(), (GemEditorViewModel vm) =>
        {
            vm.Connection = Connection;
            vm.ItemsContainer = ItemsContainer;
        });
    }
    private void ShowAddItemEditor()
    {
        ShowDialog(new CommonItemEditorWindow(), (CommonItemEditorViewModel vm) =>
        {
            vm.Connection = Connection;
            vm.RoleBagType = ItemsContainer.RoleBagType;
            vm.ItemsContainer = ItemsContainer;
        });
    }
}
