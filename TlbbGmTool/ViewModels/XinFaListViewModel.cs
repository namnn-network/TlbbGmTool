using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using liuguang.TlbbGmTool.Common;
using liuguang.TlbbGmTool.Views.XinFa;
using MySql.Data.MySqlClient;

namespace liuguang.TlbbGmTool.ViewModels;

public class XinFaListViewModel : ViewModelBase
{
    #region Fields
    public int CharGuid;
    /// <summary>
    /// 数据库连接
    /// </summary>
    public DbConnection? Connection;

    #endregion

    #region Properties

    public ObservableCollection<XinFaLogViewModel> XinFaList { get; } = new();

    public Command EditXinFaCommand { get; }

    #endregion

    public XinFaListViewModel()
    {
        EditXinFaCommand = new(ShowXinFaEditor);
    }

    public async Task LoadXinFaListAsync()
    {
        if (Connection is null)
        {
            return;
        }
        try
        {
            var xinFaList = await Task.Run(async () =>
            {
                return await DoLoadXinFaListAsync(Connection, CharGuid);
            });
            XinFaList.Clear();
            foreach (var xinFaInfo in xinFaList)
            {
                XinFaList.Add(xinFaInfo);
            }
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Tải dữ liệu không thành công!", ex);
        }
    }

    private async Task<List<XinFaLogViewModel>> DoLoadXinFaListAsync(DbConnection connection, int charGuid)
    {
        var xinFaList = new List<XinFaLogViewModel>();
        const string sql = "SELECT * FROM t_xinfa WHERE charguid=@charguid ORDER BY aid ASC";
        var mySqlCommand = new MySqlCommand(sql, connection.Conn);
        mySqlCommand.Parameters.Add(new MySqlParameter("@charguid", MySqlDbType.Int32)
        {
            Value = charGuid
        });
        // 切换数据库
        await connection.SwitchGameDbAsync();
        using var reader = await mySqlCommand.ExecuteReaderAsync();
        if (reader is MySqlDataReader rd)
        {
            while (await rd.ReadAsync())
            {
                xinFaList.Add(new(new()
                {
                    Id = rd.GetInt32("aid"),
                    CharGuid = rd.GetInt32("charguid"),
                    XinFaId = rd.GetInt32("xinfaid"),
                    XinFaLevel = rd.GetInt32("xinfalvl")
                }));
            }
        }
        return xinFaList;
    }

    private void ShowXinFaEditor(object? parameter)
    {
        if (parameter is XinFaLogViewModel xinFaLog)
        {
            ShowDialog(new XinFaEditorWindow(), (XinFaEditorViewModel vm) =>
            {
                vm.XinFaLog = xinFaLog;
                vm.Connection = Connection;
            });
        }
    }
}
