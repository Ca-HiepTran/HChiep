using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using HChiep.Place.HChiep.Place;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace HChiep.Place
{
    public class ViewModel : BaseViewModel
    {
        private Document _document;
        private UIDocument _uiDocument;
        private wpf_place _window;

        public ObservableCollection<FamilySymbol> BeamTypes { get; set; }
        public ObservableCollection<FamilySymbol> ColumnTypes { get; set; }
        public ObservableCollection<Level> Levels { get; set; }

        private FamilySymbol _selectedBeamType;
        public FamilySymbol SelectedBeamType
        {
            get => _selectedBeamType;
            set
            {
                _selectedBeamType = value;
                OnPropertyChanged(nameof(SelectedBeamType));
            }
        }
        private Level _selectedBeamLevel;
        public Level SelectedBeamLevel
        {
            get => _selectedBeamLevel;
            set
            {
                _selectedBeamLevel = value;
                OnPropertyChanged(nameof(SelectedBeamLevel));
            }
        }
        private double _length;
        public double Length
        {
            get => _length;
            set
            {
                _length = value;
                OnPropertyChanged(nameof(Length));
            }
        }

        private double _offset;
        public double Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                OnPropertyChanged(nameof(Offset));
            }
        }

        private FamilySymbol _selectedColumnType;
        public FamilySymbol SelectedColumnType
        {
            get => _selectedColumnType;
            set
            {
                _selectedColumnType = value;
                OnPropertyChanged(nameof(SelectedColumnType));
            }
        }

        private Level _selectedBaseLevel;
        public Level SelectedBaseLevel
        {
            get => _selectedBaseLevel;
            set
            {
                _selectedBaseLevel = value;
                OnPropertyChanged(nameof(SelectedBaseLevel));
            }
        }

        private Level _selectedTopLevel;
        public Level SelectedTopLevel
        {
            get => _selectedTopLevel;
            set
            {
                _selectedTopLevel = value;
                OnPropertyChanged(nameof(SelectedTopLevel));
            }
        }

        private double _baseOffset;
        public double BaseOffset
        {
            get => _baseOffset;
            set
            {
                _baseOffset = value;
                OnPropertyChanged(nameof(BaseOffset));
            }
        }

        private double _topOffset;
        public double TopOffset
        {
            get => _topOffset;
            set
            {
                _topOffset = value;
                OnPropertyChanged(nameof(TopOffset));
            }
        }

        public ICommand PlaceColumnCommand { get; private set; }
        public ICommand PlaceBeamCommand { get; private set; }

        public ViewModel(Document document, UIDocument uiDocument, wpf_place window)
        {
            _document = document;
            _uiDocument = uiDocument;
            _window = window;

            LoadBeamTypes();
            LoadColumnTypes();
            LoadLevels();
            //BeamTypes = new ObservableCollection<FamilySymbol>();
            //Levels = new ObservableCollection<Level>();

            PlaceColumnCommand = new RelayCommand(param => PlaceColumn(), CanPlaceColumn);
            PlaceBeamCommand = new RelayCommand(param => PlaceBeam(), CanPlaceBeam);

        }

        private void LoadBeamTypes()
        {
            var beamTypes = new FilteredElementCollector(_document)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                
                .ToList();

            BeamTypes = new ObservableCollection<FamilySymbol>(beamTypes);
        }

        private void LoadColumnTypes()
        {
            var columnTypes = new FilteredElementCollector(_document)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .ToList();

            ColumnTypes = new ObservableCollection<FamilySymbol>(columnTypes);
        }

        private void LoadLevels()
        {
            var levels = new FilteredElementCollector(_document)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            Levels = new ObservableCollection<Level>(levels);
        }

        private bool CanPlaceColumn(object parameter)
        {
            return SelectedColumnType != null && SelectedBaseLevel != null && SelectedTopLevel != null;
        }

        private void PlaceColumn()
        {
            try
            {
                if (SelectedColumnType == null || SelectedBaseLevel == null || SelectedTopLevel == null)
                {
                    MessageBox.Show("Please select all required fields.");
                    return;
                }

                // Temporarily hide the window to allow for point selection
                _window.Hide();

                XYZ pickPoint;
                try
                {
                    pickPoint = _uiDocument.Selection.PickPoint("Select a point to place the column");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    MessageBox.Show("Operation cancelled.");
                    _window.Show(); // Show the window again
                    return;
                }

                using (Transaction trans = new Transaction(_document, "Place Column"))
                {
                    trans.Start();

                    // Convert mm to Revit internal units (feet)
                    double baseOffsetInFeet = BaseOffset / 304.8; // Base offset (mm to feet)
                    double topOffsetInFeet = TopOffset / 304.8;   // Top offset (mm to feet)

                    // Calculate base and top level elevations
                    double baseLevelElevation = SelectedBaseLevel.Elevation + baseOffsetInFeet;
                    double topLevelElevation = SelectedTopLevel.Elevation + topOffsetInFeet;

                    // Set base and top points for column
                    XYZ basePoint = new XYZ(pickPoint.X, pickPoint.Y, baseLevelElevation);
                    XYZ topPoint = new XYZ(pickPoint.X, pickPoint.Y, topLevelElevation);

                    FamilySymbol columnSymbol = SelectedColumnType;
                    if (!columnSymbol.IsActive)
                    {
                        columnSymbol.Activate();
                        _document.Regenerate();
                    }

                    FamilyInstance columnInstance = _document.Create.NewFamilyInstance(
                        basePoint,
                        columnSymbol,
                        SelectedBaseLevel,
                        StructuralType.Column
                    );

                    // Set the top level and top offset for the column
                    Parameter topConstraintParam = columnInstance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
                    Parameter topOffsetParam = columnInstance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);

                    if (topConstraintParam != null)
                    {
                        topConstraintParam.Set(SelectedTopLevel.Id);
                    }
                    if (topOffsetParam != null)
                    {
                        topOffsetParam.Set(topOffsetInFeet);
                    }

                    Parameter baseConstraintParam = columnInstance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
                    Parameter baseOffsetParam = columnInstance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);

                    if (baseConstraintParam != null)
                    {
                        baseConstraintParam.Set(SelectedBaseLevel.Id);
                    }
                    if (baseOffsetParam != null)
                    {
                        baseOffsetParam.Set(baseOffsetInFeet);
                    }

                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}");
            }
            finally
            {
                // Show the window again on the UI thread
                _window.Dispatcher.Invoke(() => _window.Show());
            }
        }


        private bool CanPlaceBeam(object parameter)
        {
            return SelectedBeamType != null && SelectedBeamLevel != null;
        }

        private void PlaceBeam()
        {
            try
            {
                if (SelectedBeamType == null || SelectedBeamLevel == null)
                {
                    MessageBox.Show("Please select all required fields.");
                    return;
                }

                // Temporarily hide the window to allow for point selection
                _window.Hide();

                XYZ startPoint;
                XYZ endPoint;
                try
                {
                    // Pick the start point
                    startPoint = _uiDocument.Selection.PickPoint("Select the start point of the beam");

                    // Pick the end point
                    endPoint = _uiDocument.Selection.PickPoint("Select the end point of the beam");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    MessageBox.Show("Operation cancelled.");
                    _window.Show(); // Show the window again
                    return;
                }

                using (Transaction trans = new Transaction(_document, "Place Beam"))
                {
                    trans.Start();

                    // Convert offset from mm to Revit internal units (feet)
                    double offsetInFeet = Offset / 304.8;

                    // Get the elevation of the selected level
                    double levelElevation = SelectedBeamLevel.Elevation;

                    // Adjust the Z-coordinate of start and end points to match the selected level's elevation
                    startPoint = new XYZ(startPoint.X, startPoint.Y, levelElevation);
                    endPoint = new XYZ(endPoint.X, endPoint.Y, levelElevation);

                    FamilySymbol beamSymbol = SelectedBeamType;
                    if (!beamSymbol.IsActive)
                    {
                        beamSymbol.Activate();
                        _document.Regenerate();
                    }

                    // Create a new beam at the selected level
                    FamilyInstance beamInstance = _document.Create.NewFamilyInstance(
                        startPoint,
                        beamSymbol,
                        SelectedBeamLevel,
                        StructuralType.Beam
                    );

                    // Set the Z Offset Value of the beam
                    Parameter zOffsetParam = beamInstance.get_Parameter(BuiltInParameter.Z_OFFSET_VALUE);
                    if (zOffsetParam != null)
                    {
                        zOffsetParam.Set(offsetInFeet);
                    }
                    else
                    {
                        MessageBox.Show("Z Offset parameter not found on this beam family.");
                    }

                    // Adjust beam's length based on the start and end points
                    LocationCurve locationCurve = beamInstance.Location as LocationCurve;
                    if (locationCurve != null)
                    {
                        locationCurve.Curve = Line.CreateBound(startPoint, endPoint);
                    }

                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}");
            }
            finally
            {
                // Show the window again on the UI thread
                _window.Dispatcher.Invoke(() => _window.Show());
            }
        }









    }
}
