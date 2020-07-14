﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MySql.Data.MySqlClient;
using TlbbGmTool.Core;
using TlbbGmTool.Models;
using TlbbGmTool.Services;

namespace TlbbGmTool.ViewModels
{
    public class MainWindowViewModel : BindDataBase
    {
        #region Fields

        private GameServer _selectedServer;

        private DatabaseConnectionStatus _connectionStatus = DatabaseConnectionStatus.NoConnection;

        /// <summary>
        /// MySQL连接
        /// </summary>
        private MySqlConnection _mySqlConnection;

        #endregion

        #region Properties

        /// <summary>
        /// server list
        /// </summary>
        public ObservableCollection<GameServer> ServerList { get; } = new ObservableCollection<GameServer>();

        /// <summary>
        /// selected server
        /// </summary>
        public GameServer SelectedServer
        {
            get => _selectedServer;
            set => SetProperty(ref _selectedServer, value);
        }

        public DatabaseConnectionStatus ConnectionStatus
        {
            get => _connectionStatus;
            private set
            {
                if (!SetProperty(ref _connectionStatus, value))
                {
                    return;
                }

                RaisePropertyChanged(nameof(CanSelectServer));
                RaisePropertyChanged(nameof(CanDisconnectServer));
                ConnectCommand?.RaiseCanExecuteChanged();
                DisconnectCommand?.RaiseCanExecuteChanged();
            }
        }

        public bool CanSelectServer =>
            ServerList.Count > 0 && _connectionStatus == DatabaseConnectionStatus.NoConnection;

        public bool CanDisconnectServer => _connectionStatus == DatabaseConnectionStatus.Connected;

        /// <summary>
        /// 连接命令
        /// </summary>
        public AppCommand ConnectCommand { get; }

        /// <summary>
        /// 断开命令
        /// </summary>
        public AppCommand DisconnectCommand { get; }

        public MySqlConnection MySqlConnection
        {
            get => _mySqlConnection;
            private set
            {
                if (SetProperty(ref _mySqlConnection, value))
                {
                    ConnectionStatus = value == null
                        ? DatabaseConnectionStatus.NoConnection
                        : DatabaseConnectionStatus.Connected;
                }
            }
        }

        #endregion

        public MainWindowViewModel()
        {
            ConnectCommand = new AppCommand(ConnectServer,
                () => CanSelectServer);
            DisconnectCommand = new AppCommand(DisconnectServer,
                () => CanDisconnectServer);
            ServerList.CollectionChanged += (sender, e)
                =>
            {
                RaisePropertyChanged(nameof(CanSelectServer));
                ConnectCommand.RaiseCanExecuteChanged();
            };
        }

        public async Task LoadApplicationData()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var configFilePath = Path.Combine(baseDir, "config", "servers.xml");
            var servers = await ServerService.LoadGameServers(configFilePath);
            foreach (var server in servers)
            {
                ServerList.Add(server);
            }

            if (ServerList.Count > 0)
            {
                SelectedServer = ServerList.First();
            }
        }

        public void showErrorMessage(string title, string content)
        {
            MessageBox.Show(content, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async void ConnectServer()
        {
            if (SelectedServer == null)
            {
                return;
            }

            var connectionStringBuilder = new MySqlConnectionStringBuilder
            {
                Server = _selectedServer.DbHost,
                Port = _selectedServer.DbPort,
                Database = _selectedServer.AccountDbName,
                UserID = _selectedServer.DbUser,
                Password = _selectedServer.DbPassword
            };

            var mySqlConnection = new MySqlConnection
            {
                ConnectionString = connectionStringBuilder.GetConnectionString(true),
            };
            try
            {
                ConnectionStatus = DatabaseConnectionStatus.Pending;
                await Task.Run(async () => await mySqlConnection.OpenAsync());
            }
            catch (Exception e)
            {
                ConnectionStatus = DatabaseConnectionStatus.NoConnection;
                showErrorMessage("连接数据库出错", e.Message);
                return;
            }

            MySqlConnection = mySqlConnection;
            SelectedServer.Connected = true;
        }

        private async void DisconnectServer()
        {
            if (SelectedServer == null)
            {
                return;
            }

            try
            {
                ConnectionStatus = DatabaseConnectionStatus.Pending;
                await _mySqlConnection.CloseAsync();
            }
            catch (Exception e)
            {
                showErrorMessage("断开连接出错", e.Message);
            }

            MySqlConnection = null;
            SelectedServer.Connected = false;
        }
    }
}