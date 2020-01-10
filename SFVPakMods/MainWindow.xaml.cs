using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SFVPakMods
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private class PakFile : INotifyPropertyChanged
        {
            private string _name;
            private string _path;
            private bool _enabled;

            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(string info)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
            }

            public string Name { get { return _name; } set { _name = value; NotifyPropertyChanged("Name"); } }
            public string ModPath { get { return _path; } set { _path = value; _name = Path.GetFileNameWithoutExtension(value).Substring(3); } }
            public bool Enabled { get { return _enabled; } set { _enabled = value; NotifyPropertyChanged("Enabled"); } }
        }

        private ObservableCollection<PakFile> paksList;
        private readonly string modsDir = ConfigurationManager.AppSettings["modsPath"];

        private void MDMessageBox(string text)
        {
            mainDialog.DialogContent = new TextBlock
            {
                Text = text,
                TextAlignment = TextAlignment.Center,
                FontSize = 18,
                Margin = new Thickness(20)
            };
            mainDialog.IsOpen = true;
        }

        private void UpdateFileNames(ObservableCollection<PakFile> currentList)
        {
            mainList.IsEnabled = false;
            for (int j = 0; j < currentList.Count; j++)
            {
                string newext;
                if (currentList[j].Enabled) newext = ".pak";
                else newext = ".disabled";
                string newpath = Path.Combine(modsDir, (j + 1).ToString().PadLeft(2, '0') + "_" + currentList[j].Name + newext);
                try
                {
                    if (currentList[j].ModPath != newpath)
                    {
                        File.Move(currentList[j].ModPath, newpath);
                        currentList[j].ModPath = newpath;
                    }
                }
                catch (Exception e)
                {
                    MDMessageBox("Something went wrong!");
                    Environment.Exit(0);
                }                
            }
            mainList.IsEnabled = true;
        }

        private void LoadFileNames(ObservableCollection<PakFile> currentList)
        {
            currentList.Clear();
            var templist = Directory.EnumerateFiles(modsDir).Where(s => s.EndsWith(".pak") || s.EndsWith(".disabled"));
            int i = 0;
            foreach (string fl in templist)
            {
                i++;
                Match m = Regex.Match(Path.GetFileName(fl), @"\d\d[_]");
                string pathname = fl;
                if (!m.Success)
                {
                    pathname = Path.Combine(modsDir, i.ToString().PadLeft(2, '0') + "_" + Path.GetFileName(fl));
                    try
                    {
                        File.Move(fl, pathname);
                    }
                    catch (Exception e)
                    {
                        MDMessageBox("Something went wrong");
                        Environment.Exit(0);
                    }
                }
                currentList.Add(new PakFile { Enabled = (Path.GetExtension(fl) == ".pak"), ModPath = pathname });
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            if (Directory.Exists(modsDir))
            {
                paksList = new ObservableCollection<PakFile>();

                LoadFileNames(paksList);
                UpdateFileNames(paksList);

                mainList.ItemsSource = paksList;
            }
            else
            {
                mainList.IsEnabled = false;
                MDMessageBox("Specify in config file the valid path to '~mods' folder!");
            }
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = mainList.SelectedIndex;
            
            if (selectedIndex > 0)
            {
                PakFile itemToMoveUp = paksList[selectedIndex];
                paksList.RemoveAt(selectedIndex);
                paksList.Insert(selectedIndex - 1, itemToMoveUp);
                mainList.SelectedIndex = selectedIndex - 1;

                UpdateFileNames(paksList);
            }
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = mainList.SelectedIndex;

            if (selectedIndex + 1 < paksList.Count)
            {
                PakFile itemToMoveDown = paksList[selectedIndex];
                paksList.RemoveAt(selectedIndex);
                paksList.Insert(selectedIndex + 1, itemToMoveDown);
                mainList.SelectedIndex = selectedIndex + 1;
                
                UpdateFileNames(paksList);
            }
        }

        private async void Remove_Click(object sender, RoutedEventArgs e)
        {
            TextBlock txt1 = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.WrapWithOverflow,
                FontSize = 18,
                Text = "Are you sure?"
            };

            Button btn1 = new Button
            {
                Style = Application.Current.FindResource("MaterialDesignRaisedButton") as Style,
                Width = 115,
                Height = 30,
                Margin = new Thickness(10),
                Command = MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand,
                CommandParameter = true,
                Content = "Yes"
            };

            Button btn2 = new Button
            {
                Style = Application.Current.FindResource("MaterialDesignRaisedButton") as Style,
                Width = 115,
                Height = 30,
                Margin = new Thickness(10),
                Command = MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand,
                CommandParameter = false,
                Content = "No"
            };

            DockPanel dck = new DockPanel();
            dck.Children.Add(btn1);
            dck.Children.Add(btn2);

            StackPanel stk = new StackPanel
            {
                Width = 270
            };
            stk.Children.Add(txt1);
            stk.Children.Add(dck);

            object result = await MaterialDesignThemes.Wpf.DialogHost.Show(stk);
            
            if (result is bool boolResult && boolResult)
            {
                var selectedIndex = mainList.SelectedIndex;
                try
                {
                    FileSystem.DeleteFile(paksList[selectedIndex].ModPath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
                    paksList.RemoveAt(selectedIndex);
                    
                    UpdateFileNames(paksList);
                    LoadFileNames(paksList);
                }
                catch (Exception er)
                {
                    MDMessageBox("Something went wrong");
                    Environment.Exit(0);
                }
            }
        }

        private void Txt1_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (Path.GetInvalidFileNameChars().Contains(e.Text.ToCharArray()[0]))
            {
                e.Handled = true;
            }
        }

        private async void Rename_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = mainList.SelectedIndex;
            TextBox txt1 = new TextBox
            {
                Width = 380,
                SelectionBrush = Application.Current.FindResource("PrimaryHueMidBrush") as Brush,
                CaretBrush = Application.Current.FindResource("MaterialDesignBody") as Brush,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.WrapWithOverflow,
                FontSize = 18,
                Text = paksList[selectedIndex].Name
            };
            txt1.PreviewTextInput += Txt1_PreviewTextInput;

            Button btn1 = new Button
            {
                Style = Application.Current.FindResource("MaterialDesignRaisedButton") as Style,
                Width = 180,
                Height = 30,
                Margin = new Thickness(10),
                Command = MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand,
                CommandParameter = true,
                Content = "Rename"
            };

            Button btn2 = new Button
            {
                Style = Application.Current.FindResource("MaterialDesignRaisedButton") as Style,
                Width = 180,
                Height = 30,
                Margin = new Thickness(10),
                Command = MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand,
                CommandParameter = false,
                Content = "Cancel"
            };

            DockPanel dck = new DockPanel();
            dck.Children.Add(btn1);
            dck.Children.Add(btn2);

            StackPanel stk = new StackPanel
            {
                Width = 400
            };
            stk.Children.Add(txt1);
            stk.Children.Add(dck);

            txt1.Focus();
            txt1.SelectAll();

            object result = await MaterialDesignThemes.Wpf.DialogHost.Show(stk);

            if (result is bool boolResult && boolResult)
            {
                paksList[selectedIndex].Name = txt1.Text.Replace(" ","_");
                UpdateFileNames(paksList);
            }
        }

        private DependencyObject GetParentDependencyObjectFromVisualTree(DependencyObject startObject, Type type)
        {
            DependencyObject parent = startObject;
            while (parent != null)
            {
                if (type.IsInstanceOfType(parent))
                    break;
                else
                    parent = VisualTreeHelper.GetParent(parent);
            }
            return parent;
        }

        private void MainList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem clickedOnItem = (ListBoxItem)GetParentDependencyObjectFromVisualTree((DependencyObject)e.MouseDevice.DirectlyOver, typeof(ListBoxItem));

            if (clickedOnItem != null)
            {
                if (!clickedOnItem.IsSelected)
                {
                    clickedOnItem.IsSelected = true;
                    clickedOnItem.Focus();
                }
            }
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateFileNames(paksList);
        }

        private void MainList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    foreach (string fl in files)
                    {
                        if (fl.EndsWith(".pak"))
                            FileSystem.CopyFile(fl, Path.Combine(modsDir, Path.GetFileName(fl)), UIOption.AllDialogs, UICancelOption.DoNothing);
                    }
                    LoadFileNames(paksList);
                    UpdateFileNames(paksList);
                }
                catch (Exception er)
                {
                    MDMessageBox("Something went wrong");
                }
            }
        }

        private void MenuReload_Click(object sender, RoutedEventArgs e)
        {
            LoadFileNames(paksList);
            UpdateFileNames(paksList);
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(modsDir);
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            MDMessageBox("SFVPakMods\n\nDeveloped by github.com/eddiezato\nIcon by mattahan.com\nTheme by Google & materialdesigninxaml.net");
        }
    }
}
