﻿using CSharp_GitBranchManager.Models;
using CSharp_GitBranchManager.Utils;
using LibGit2Sharp;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace CSharp_GitBranchManager.ViewModels
{
    public interface IConfigurationViewModel
    {
        Models.Configuration Configuration { get; set; }
        ObservableCollection<string> RemoteBranches { get; }

        #region Commands

        ICommand ReleadRemoteBranchesCommand { get; }
        ICommand SaveCommand { get; }
        ICommand SelectRepositoryPathCommand { get; }
        ICommand ValidateTextInputCommand { get; }

        #endregion Commands
    }

    public class ConfigurationViewModel : ANotifyPropertyChanged, IConfigurationViewModel
    {
        private readonly JsonSerializerOptions _loadOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly ObservableCollection<string> _remoteBranches;
        private readonly JsonSerializerOptions _saveOptions = new() { WriteIndented = true };
        private Models.Configuration _configuration;

        #region Commands

        public ICommand ReleadRemoteBranchesCommand => new RelayCommand<object>(ReleadRemoteBranches);
        public ICommand SaveCommand => new RelayCommand<object>(Save);
        public ICommand SelectRepositoryPathCommand => new RelayCommand<object>(SelectRepositoryPath);
        public ICommand ValidateTextInputCommand => new RelayCommand<string>(ValidateTextInput);

        #endregion Commands

        #region Properties

        public Models.Configuration Configuration
        {
            get => _configuration;
            set => SetField(ref _configuration, value);
        }

        public ObservableCollection<string> RemoteBranches
        {
            get { return _remoteBranches; }
        }

        #endregion Properties

        public ConfigurationViewModel(Models.Configuration appConfiguration)
        {
            _configuration = appConfiguration;
            _remoteBranches = ["master"];
            Load();
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(Models.Configuration.FilePath)) return;

                string json = File.ReadAllText(Models.Configuration.FilePath);
                var config = JsonSerializer.Deserialize<Models.Configuration>(json, _loadOptions);
                _remoteBranches.Clear();
                _remoteBranches.Add(config.RemoteMergedBranch);
                Configuration = config;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading config: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReleadRemoteBranches(object parameter)
        {
            using (var repo = new Repository(Configuration.GitRepositoryPath))
            {
                var branches = repo.Branches.Where(b => b.IsRemote).ToList();

                var value = Configuration.RemoteMergedBranch;

                _remoteBranches.Clear();

                foreach (var branch in branches)
                {
                    _remoteBranches.Add(branch.FriendlyName.Replace("origin/", ""));
                }

                Configuration.RemoteMergedBranch = value;
            }
        }

        private void Save(object parameter)
        {
            try
            {
                var jsonString = JsonSerializer.Serialize(Configuration, _saveOptions);
                File.WriteAllText(Models.Configuration.FilePath, jsonString);
                MessageBox.Show("Config saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving config: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectRepositoryPath(object parameter)
        {
            var folderBrowserDialog = new FolderBrowserDialog
            {
                SelectedPath = Configuration.GitRepositoryPath ?? string.Empty
            };

            DialogResult result = folderBrowserDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                Configuration.GitRepositoryPath = folderBrowserDialog.SelectedPath;
                OnPropertyChanged(nameof(Configuration));
            }
        }

        private void ValidateTextInput(string input)
        {
            if (!string.IsNullOrEmpty(input) && input.Any(c => !char.IsDigit(c)))
            {
                MessageBox.Show("Only numeric characters are allowed.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}