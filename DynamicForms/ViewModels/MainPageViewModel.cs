using DynamicForms.Models.Data;
using DynamicForms.Models.Definitions;
using DynamicForms.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace DynamicForms.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private readonly FormActionService _formActions = new FormActionService();

        private const string DataFileName = "ToolSetupForm.data.json";
        private const string DataAssetUri = "ms-appx:///Assets/ToolSetupForm.data.json";
        private const string StructureAssetUri = "ms-appx:///Assets/ToolSetupForm.structure.json";

        private DynamicFormViewModel _dynamicForm;
        private FormDefinition _formDefinition;
        private FormDataContext _dataContext;

        public DynamicFormViewModel DynamicForm
        {
            get => _dynamicForm;
            private set
            {
                if (_dynamicForm != value)
                {
                    _dynamicForm = value;
                    RaisePropertyChanged();
                }
            }
        }

        public FormDefinition FormDefinition => _formDefinition;
        public FormDataContext DataContext => _dataContext;

        public async Task InitializeAsync()
        {
            try
            {
                // Ensure we have a persisted data file in LocalFolder
                StorageFile dataFile = await EnsureLocalDataFileAsync();

                // Load DATA json from local storage
                string dataJson = await FileIO.ReadTextAsync(dataFile);
                JObject dataRoot = JObject.Parse(dataJson);
                _dataContext = new FormDataContext(dataRoot);

                // Load STRUCTURE json from app package (read-only)
                StorageFile structFile = await StorageFile.GetFileFromApplicationUriAsync(
                    new Uri(StructureAssetUri));
                string structJson = await FileIO.ReadTextAsync(structFile);
                _formDefinition = FormDefinitionFactory.FromJson(structJson);

                // Create root dynamic form VM
                RebuildDynamicForm();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing MainPageViewModel: {ex}");
            }
        }

        /// <summary>
        /// Rebuilds the DynamicForm view-model from the current
        /// FormDefinition and FormDataContext without reloading JSON from disk.
        /// </summary>
        public void RebuildDynamicForm()
        {
            if (_formDefinition == null || _dataContext == null)
                return;

            DynamicForm = new DynamicFormViewModel(_formDefinition, _dataContext);
        }

        /// <summary>
        /// Persists the current data context JSON to the local data file.
        /// Call this after any operation that changes the JSON (save, edit, etc.).
        /// </summary>
        public async Task SaveDataAsync()
        {
            if (_dataContext?.Root == null)
                return;

            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile dataFile = await localFolder.CreateFileAsync(
                    DataFileName,
                    CreationCollisionOption.OpenIfExists);

                string json = JsonConvert.SerializeObject(_dataContext.Root, Formatting.Indented);
                await FileIO.WriteTextAsync(dataFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving data JSON: {ex}");
            }
        }

        private static async Task<StorageFile> EnsureLocalDataFileAsync()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            // Try to get existing local data file
            StorageFile existing;
            try
            {
                existing = await localFolder.GetFileAsync(DataFileName);
                return existing;
            }
            catch
            {
                // Not found, fall back to creating from asset template
            }

            // Copy initial data from the packaged asset
            StorageFile assetFile = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri(DataAssetUri));

            StorageFile localCopy = await assetFile.CopyAsync(
                localFolder,
                DataFileName,
                NameCollisionOption.ReplaceExisting);

            return localCopy;
        }

        public async Task HandleSaveNewEntryAsync(ActionViewModel actionVm)
        {
            if (DynamicForm?.Root == null || actionVm == null)
                return;

            await _formActions.SaveNewEntryAsync(DynamicForm.Root, actionVm);
            await SaveDataAsync();
        }

        public async Task HandleSaveRowEditAsync(ActionViewModel actionVm)
        {
            if (DynamicForm?.Root == null || actionVm == null)
                return;

            await _formActions.SaveRowEditAsync(DynamicForm.Root, actionVm);
            await SaveDataAsync();
        }

        public async Task HandleEnterEditModeAsync(ActionViewModel actionVm)
        {
            if (actionVm == null)
                return;

            _formActions.EnterEditMode(actionVm);
            await SaveDataAsync();
        }
    }
}