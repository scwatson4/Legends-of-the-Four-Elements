using AnythingWorld.Behaviour.Tree;
using AnythingWorld.Networking.Editor;
using AnythingWorld.Utilities;
using AnythingWorld.Utilities.Data;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnythingWorld.Models;
using Cysharp.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using AnythingWorld.Behaviour;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace AnythingWorld.Editor
{
    [Serializable]
    public class StateTexture2D
    {
        public Texture2D activeTexture;
        public Texture2D inactiveTexture;
        public Texture2D hoverTexture;

        public bool TexturesLoadedNoHover => activeTexture != null && inactiveTexture != null;
        public bool TexturesLoadedHover => activeTexture != null && inactiveTexture != null && hoverTexture != null;

        public StateTexture2D(Texture2D activeTexture, Texture2D inactiveTexture, Texture2D hoverTexture = null)
        {
            this.activeTexture = activeTexture;
            this.inactiveTexture = inactiveTexture;
            this.hoverTexture = hoverTexture;
        }
    }

    public class AnythingCreatorEditor : AnythingEditor
    {
        #region Fields
        public enum CreationSearchCategory
        {
            MODELS, WORLDS, LIGHTING, COLLECTION
        }

        private const float timeToWaitIcon = 1f;
        protected string windowTitle;
        private float searchRingAngle;
        private double lastEditorTime;
        protected float resultThumbnailMultiplier = 1f;
        
        protected static List<SearchResult> searchResults;
        
        protected List<SearchResult> filteredResults;
        protected SearchResult selectedResult;
        private SearchResult lastResult;
        protected enum SearchMode { IDLE, RUNNING, RUNNING_SILENTLY, FAILURE, SUCCESS }

        protected SearchMode searchMode = SearchMode.IDLE;
        protected string searchModeFailReason = "";

        protected Color bannerTintA, bannerTintB;

        protected bool isDragging;
        protected Texture2D textureThumb;
        protected bool buttonPressed;

        public static Dictionary<DefaultBehaviourType, BehaviourTree> DefaultBehaviourDictionary
        {
            get
            {
                var temp = new Dictionary<DefaultBehaviourType, BehaviourTree>();

                if (TransformSettings.GroundCreatureBehaviourTree != null) 
                    temp.Add(DefaultBehaviourType.GroundCreature, TransformSettings.GroundCreatureBehaviourTree);
                if (TransformSettings.GroundVehicleBehaviourTree != null) 
                    temp.Add(DefaultBehaviourType.GroundVehicle, TransformSettings.GroundVehicleBehaviourTree);
                if (TransformSettings.FlyingCreatureBehaviourTree != null) 
                    temp.Add(DefaultBehaviourType.FlyingCreature, TransformSettings.FlyingCreatureBehaviourTree);
                if (TransformSettings.FlyingVehicleBehaviourTree != null) 
                    temp.Add(DefaultBehaviourType.FlyingVehicle, TransformSettings.FlyingVehicleBehaviourTree);
                if (TransformSettings.SwimmingCreatureBehaviourTree != null) 
                    temp.Add(DefaultBehaviourType.SwimmingCreature, TransformSettings.SwimmingCreatureBehaviourTree);
                if (TransformSettings.StaticBehaviourTree != null) 
                    temp.Add(DefaultBehaviourType.Static, TransformSettings.StaticBehaviourTree);
                
                return temp;
            }
        }
        #region Filters
        protected DropdownOption[] CategoryFilter
        {
            get
            {
                string[] categoryLabels = {
                    "All", "Animals & Pets", "Architecture", "Art & Abstract",
            "Cars & Vehicles", "Characters & Creatures", "Cultural Heritage & History",
            "Electronics & Gadgets","Fashion & Style","Food & Drink","Furniture & Home",
            "Music","Nature & Plants","News & Politics","People","Places & Travel",
            "Science & Technology","Sports & Fitness","Weapons & Military"};
                if (categoryFilter == null)
                {
                    var dropdownList = new List<DropdownOption>();
                    foreach (var (label, index) in categoryLabels.WithIndex())
                    {
                        var option = new DropdownOption()
                        {
                            dataEndpoint = (CategoryDropdownOption)index,
                            label = categoryLabels[index],
                            function = () =>
                            {
                                currentCategory = (CategoryDropdownOption)index;
                                FilterSearchResult(searchResults);
                            }
                        };

                        dropdownList.Add(option);
                    }
                    categoryFilter = dropdownList.ToArray();
                }
                return categoryFilter;
            }
        }
        protected DropdownOption[] categoryFilter;

        protected DropdownOption[] AnimationFilter
        {
            get
            {
                if (animationFilter == null)
                {
                    string[] animationLabels = { "Animated & Still", "Animated Only", "Still Only" };
                    var dropdownList = new List<DropdownOption>();
                    foreach (var (label, index) in animationLabels.WithIndex())
                    {
                        var option = new DropdownOption()
                        {
                            dataEndpoint = (AnimatedDropdownOption)index,
                            label = animationLabels[index],
                            function = () =>
                            {
                                currentAnimationFilter = (AnimatedDropdownOption)index;
                                FilterSearchResult(searchResults);
                            }
                        };

                        dropdownList.Add(option);
                    }
                    animationFilter = dropdownList.ToArray();
                }
                return animationFilter;
            }
        }
        protected DropdownOption[] animationFilter;

        protected DropdownOption[] SortingFilter
        {
            get
            {
                if (sortingFilter == null)
                {
                    string[] sortingLabels = { "Most Relevant", "Most Popular", "Most Liked", "A-Z", "Z-A", "Liked Models" };
                    var dropdownList = new List<DropdownOption>();
                    foreach (var (label, index) in sortingLabels.WithIndex())
                    {
                        var option = new DropdownOption()
                        {
                            dataEndpoint = (SortingDropdownOption)index,
                            label = sortingLabels[index],
                            function = () =>
                            {
                                currentSortingMethod = (SortingDropdownOption)index;
                                FilterSearchResult(searchResults);
                            }
                        };

                        dropdownList.Add(option);
                    }
                    sortingFilter = dropdownList.ToArray();
                }
                return sortingFilter;
            }
        }
        protected DropdownOption[] sortingFilter;

        protected enum CategoryDropdownOption
        {
            ALL, 
            ANIMAL, 
            ARCHITECTURE, 
            ART, 
            VEHICLE, 
            CHARACTER, 
            CULTURE, 
            ELECTRONIC, 
            FASHION, FOOD, 
            FURNITURE, 
            MUSIC, 
            NATURE, 
            NEWS, 
            PEOPLE, 
            PLACE, 
            SCIENCE, 
            SPORTS, 
            WEAPON
        }
        
        protected CategoryDropdownOption currentCategory = CategoryDropdownOption.ALL;

        protected enum AnimatedDropdownOption { BOTH, ANIMATED, STILL }
        protected AnimatedDropdownOption currentAnimationFilter = AnimatedDropdownOption.BOTH;

        protected SortingDropdownOption currentSortingMethod = SortingDropdownOption.MostRelevant;
        #endregion Filters
        #region Styles
        protected GUIStyle IconStyle;

        protected GUIStyle ModelNameStyle;
        protected GUIStyle AuthorNameStyle;
        protected GUIStyle VoteStyle;
        #endregion Styles
        #region Textures
        #region Base Textures
        private Texture2D baseLoadingCircle;
        protected Texture2D BaseLoadingCircle
        {
            get
            {
                if (baseLoadingCircle == null)
                {
                    baseLoadingCircle = TintTextureToEditorTheme(Resources.Load("Editor/Shared/loadingCircle") as Texture2D);
                }
                return baseLoadingCircle;
            }
        }

        #region Icons
        private Texture2D baseWebsiteIcon;
        protected Texture2D BaseWebsiteIcon
        {
            get
            {
                if (baseWebsiteIcon == null)
                {
                    baseWebsiteIcon = Resources.Load("Editor/Shared/Icons/SocialIcons/website") as Texture2D;
                }
                return baseWebsiteIcon;
            }
        }

        private Texture2D baseDiscordIcon;
        protected Texture2D BaseDiscordIcon
        {
            get
            {
                if (baseDiscordIcon == null)
                {
                    baseDiscordIcon = Resources.Load("Editor/Shared/Icons/SocialIcons/discord") as Texture2D;
                }
                return baseDiscordIcon;
            }
        }

        private Texture2D baseLogoutIcon;
        protected Texture2D BaseLogoutIcon
        {
            get
            {
                if (baseLogoutIcon == null)
                {
                    baseLogoutIcon = Resources.Load("Editor/Shared/Icons/SocialIcons/logout") as Texture2D;
                }
                return baseLogoutIcon;
            }
        }


        private Texture2D baseResetIcon;
        protected Texture2D BaseResetIcon
        {
            get
            {
                if (baseResetIcon == null)
                {
                    baseResetIcon = Resources.Load("Editor/Shared/Icons/SettingsIcons/reset") as Texture2D;
                }
                return baseResetIcon;
            }
        }

        private Texture2D baseTransformIcon;
        protected Texture2D BaseTransformIcon
        {
            get
            {
                if (baseTransformIcon == null)
                {
                    baseTransformIcon = Resources.Load("Editor/Shared/Icons/SettingsIcons/transform") as Texture2D;
                }
                return baseTransformIcon;
            }
        }

        private Texture2D baseGridIcon;
        protected Texture2D BaseGridIcon
        {
            get
            {
                if (baseGridIcon == null)
                {
                    baseGridIcon = Resources.Load("Editor/Shared/Icons/SettingsIcons/grid") as Texture2D;
                }
                return baseGridIcon;
            }
        }

        private Texture2D baseClearIcon;
        protected Texture2D BaseClearIcon
        {
            get
            {
                if (baseClearIcon == null)
                {
                    baseClearIcon = Resources.Load("Editor/Shared/Icons/SettingsIcons/clear") as Texture2D;
                }
                return baseClearIcon;
            }
        }

        private Texture2D baseBackIcon;
        protected Texture2D BaseBackIcon
        {
            get
            {
                if (baseBackIcon == null)
                {
                    baseBackIcon = Resources.Load("Editor/Shared/Icons/SettingsIcons/back") as Texture2D;
                }
                return baseBackIcon;
            }
        }

        private Texture2D baseThumbnailIcon;
        protected Texture2D BaseThumbnailIcon
        {
            get
            {
                if (baseThumbnailIcon == null)
                {
                    baseThumbnailIcon = Resources.Load("Editor/Shared/Icons/SettingsIcons/thumbnail") as Texture2D;
                }

                return baseThumbnailIcon;
            }
        }


        private Texture2D baseInfoIcon;
        protected Texture2D BaseInfoIcon
        {
            get
            {
                if (baseInfoIcon == null)
                {
                    baseInfoIcon = Resources.Load("Editor/Shared/Icons/ObjectIcons/info") as Texture2D;
                }
                return baseInfoIcon;
            }
        }

        private Texture2D baseAnimatedIcon;
        protected Texture2D BaseAnimatedIcon
        {
            get
            {
                if (baseAnimatedIcon == null)
                {
                    baseAnimatedIcon = Resources.Load("Editor/Shared/Icons/ObjectIcons/animated") as Texture2D;
                }
                return baseAnimatedIcon;
            }
        }

        private Texture2D baseReportIcon;
        protected Texture2D BaseReportIcon
        {
            get
            {
                if (baseReportIcon == null)
                {
                    baseReportIcon = Resources.Load("Editor/Shared/Icons/ObjectIcons/report") as Texture2D;
                }
                return baseReportIcon;
            }
        }

        private Texture2D baseFilledHeart;
        protected Texture2D BaseFilledHeart
        {
            get
            {
                if (baseFilledHeart == null)
                {
                    baseFilledHeart = Resources.Load("Editor/Shared/Icons/ObjectIcons/filledHeart") as Texture2D;
                }
                return baseFilledHeart;
            }
        }

        private Texture2D baseEmptyHeart;
        protected Texture2D BaseEmptyHeart
        {
            get
            {
                if (baseEmptyHeart == null)
                {
                    baseEmptyHeart = Resources.Load("Editor/Shared/Icons/ObjectIcons/emptyHeart") as Texture2D;
                }
                return baseEmptyHeart;
            }
        }

        private Texture2D baseCollectionIcon;
        protected Texture2D BaseCollectionIcon
        {
            get
            {
                if (baseCollectionIcon == null)
                {
                    baseCollectionIcon = Resources.Load("Editor/Shared/Icons/ObjectIcons/collectionAddition") as Texture2D;
                }
                return baseCollectionIcon;
            }
        }


        private Texture2D baseTypeIcon;
        protected Texture2D BaseTypeIcon
        {
            get
            {
                if (baseTypeIcon == null)
                {
                    baseTypeIcon = Resources.Load("Editor/Shared/Icons/ObjectIcons/objectType") as Texture2D;
                }
                return baseTypeIcon;
            }
        }

        private Texture2D basePolyCountIcon;
        protected Texture2D BasePolyCountIcon
        {
            get
            {
                if (basePolyCountIcon == null)
                {
                    basePolyCountIcon = Resources.Load("Editor/Shared/Icons/ObjectIcons/polygonCount") as Texture2D;
                }
                return basePolyCountIcon;
            }
        }

        private Texture2D baseLicenseIcon;
        protected Texture2D BaseLicenseIcon
        {
            get
            {
                if (baseLicenseIcon == null)
                {
                    baseLicenseIcon = Resources.Load("Editor/Shared/Icons/ObjectIcons/license") as Texture2D;
                }
                return baseLicenseIcon;
            }
        }


        private Texture2D baseCategoryIcon;
        protected Texture2D BaseCategoryIcon
        {
            get
            {
                if (baseCategoryIcon == null)
                {
                    baseCategoryIcon = Resources.Load("Editor/Shared/Icons/ObjectIcons/category") as Texture2D;
                }
                return baseCategoryIcon;
            }
        }

        private Texture2D baseTagIcon;
        protected Texture2D BaseTagIcon
        {
            get
            {
                if (baseTagIcon == null)
                {
                    baseTagIcon = Resources.Load("Editor/Shared/Icons/ObjectIcons/tag") as Texture2D;
                }
                return baseTagIcon;
            }
        }

        private Texture2D baseEnvironmentIcon;
        protected Texture2D BaseEnvironmentIcon
        {
            get
            {
                if (baseEnvironmentIcon == null)
                {
                    baseEnvironmentIcon = Resources.Load("Editor/Shared/Icons/ObjectIcons/environment") as Texture2D;
                }
                return baseEnvironmentIcon;
            }
        }
        #endregion Icons
        #region Details
        private Texture2D[] baseDetailsThumbnailBackdrops;
        protected Texture2D[] BaseDetailsThumbnailBackdrops
        {
            get
            {
                if (baseDetailsThumbnailBackdrops == null || !baseDetailsThumbnailBackdrops.Any())
                {
                    baseDetailsThumbnailBackdrops = new[]
                    {
                        Resources.Load("Editor/Shared/DetailBackdrops/detailGradientBackdrop1") as Texture2D,
                        Resources.Load("Editor/Shared/DetailBackdrops/detailGradientBackdrop2") as Texture2D,
                        Resources.Load("Editor/Shared/DetailBackdrops/detailGradientBackdrop3") as Texture2D,
                        Resources.Load("Editor/Shared/DetailBackdrops/detailGradientBackdrop4") as Texture2D,
                        Resources.Load("Editor/Shared/DetailBackdrops/detailGradientBackdrop5") as Texture2D
                    };
                }
                return baseDetailsThumbnailBackdrops;
            }
        }
        #endregion Details
        #region Card
        private Texture2D baseCardFrame;
        protected Texture2D BaseCardFrame
        {
            get
            {
                if (baseCardFrame == null)
                {
                    baseCardFrame = Resources.Load("Editor/Shared/cardFrame") as Texture2D;
                }
                return baseCardFrame;
            }
        }

        private Texture2D[] baseCardThumbnailBackdrops;
        protected Texture2D[] BaseCardThumbnailBackdrops
        {
            get
            {
                if (baseCardThumbnailBackdrops == null || !baseCardThumbnailBackdrops.Any())
                {
                    baseCardThumbnailBackdrops = new[]
                    {
                        Resources.Load("Editor/Shared/CardBackdrops/cardGradientBackdrop1") as Texture2D,
                        Resources.Load("Editor/Shared/CardBackdrops/cardGradientBackdrop2") as Texture2D,
                        Resources.Load("Editor/Shared/CardBackdrops/cardGradientBackdrop3") as Texture2D,
                        Resources.Load("Editor/Shared/CardBackdrops/cardGradientBackdrop4") as Texture2D,
                        Resources.Load("Editor/Shared/CardBackdrops/cardGradientBackdrop5") as Texture2D
                    };
                }
                return baseCardThumbnailBackdrops;
            }
        }

        private Texture2D baseObjectSelectionTint;
        protected Texture2D BaseObjectSelectionTint
        {
            get
            {
                if (baseObjectSelectionTint == null)
                {
                    baseObjectSelectionTint = Resources.Load("Editor/Shared/objectSelectionTint") as Texture2D;
                }
                return baseObjectSelectionTint;
            }
        }

        private Texture2D baseUserDefaultProfile;
        protected Texture2D BaseUserDefaultProfile
        {
            get
            {
                if (baseUserDefaultProfile == null)
                {
                    baseUserDefaultProfile = Resources.Load("Editor/Shared/defaultUserProfile") as Texture2D;
                }
                return baseUserDefaultProfile;
            }
        }
        #endregion Card
        #region Author Icons
        private Texture2D userProfile_AnythingWorld;
        protected Texture2D UserProfile_AnythingWorld
        {
            get
            {
                if (userProfile_AnythingWorld == null)
                {
                    userProfile_AnythingWorld = Resources.Load("Editor/AnythingBrowser/CuratedAuthorIcons/AuthorIcon_AnythingWorld") as Texture2D;
                }
                return userProfile_AnythingWorld;
            }
        }

        private Texture2D userProfile_GooglePoly;
        protected Texture2D UserProfile_GooglePoly
        {
            get
            {
                if (userProfile_GooglePoly == null)
                {
                    userProfile_GooglePoly = Resources.Load("Editor/AnythingBrowser/CuratedAuthorIcons/AuthorIcon_GooglePoly") as Texture2D;
                }
                return userProfile_GooglePoly;
            }
        }

        private Texture2D userProfile_Quaternius;
        protected Texture2D UserProfile_Quaternius
        {
            get
            {
                if (userProfile_Quaternius == null)
                {
                    userProfile_Quaternius = Resources.Load("Editor/AnythingBrowser/CuratedAuthorIcons/AuthorIcon_Quaternius") as Texture2D;
                }
                return userProfile_Quaternius;
            }
        }
        #endregion Author Icons
        #endregion Base Textures
        #region Tinted Textures
        private Texture2D tintedCardFrame;
        protected Texture2D TintedCardFrame
        {
            get
            {
                if (tintedCardFrame == null)
                {
                    tintedCardFrame = TintTexture(BaseCardFrame, HexToColour("3F4041"));
                }
                return tintedCardFrame;
            }
        }

        private Texture2D blackAnythingGlobeLogo;
        protected Texture2D BlackAnythingGlobeLogo
        {
            get
            {
                if (blackAnythingGlobeLogo == null)
                {
                    blackAnythingGlobeLogo = TintTexture(BaseAnythingGlobeLogo, Color.black);
                }
                return blackAnythingGlobeLogo;
            }
        }

        private Texture2D tintedGradientBanner;
        protected Texture2D TintedGradientBanner
        {
            get
            {
                if (tintedGradientBanner == null)
                {
                    tintedGradientBanner = TintGradient(BaseGradientBanner, bannerTintA, bannerTintB);
                }
                return tintedGradientBanner;
            }
        }

        private Texture2D tintedThumbnailIcon;
        protected Texture2D TintedThumbnailIcon
        {
            get
            {
                if (tintedThumbnailIcon == null)
                {
                    tintedThumbnailIcon = TintTexture(BaseThumbnailIcon, HexToColour("999999"));
                }
                return tintedThumbnailIcon;
            }
        }

        private Texture2D tintedReportIcon;
        protected Texture2D TintedReportIcon
        {
            get
            {
                if (tintedReportIcon == null)
                {
                    tintedReportIcon = TintTextureToEditorTheme(BaseReportIcon);
                }
                return tintedReportIcon;
            }
        }
        #endregion Tinted Textures
        #region State Textures
        private StateTexture2D stateLogoutIcon;
        protected StateTexture2D StateLogoutIcon
        {
            get
            {
                if (stateLogoutIcon == null || !stateLogoutIcon.TexturesLoadedHover)
                {
                    stateLogoutIcon = new StateTexture2D(BaseLogoutIcon, BaseLogoutIcon, TintTexture(BaseLogoutIcon, HexToColour("EEEEEE")));
                }
                return stateLogoutIcon;
            }
            set => stateLogoutIcon = value;
        }
        private StateTexture2D stateDiscordIcon;
        protected StateTexture2D StateDiscordIcon
        {
            get
            {
                if (stateDiscordIcon == null || !stateDiscordIcon.TexturesLoadedHover)
                {
                    stateDiscordIcon = new StateTexture2D(BaseDiscordIcon, BaseDiscordIcon, TintTexture(BaseDiscordIcon, HexToColour("EEEEEE")));
                }
                return stateDiscordIcon;
            }
            set => stateDiscordIcon = value;
        }
        private StateTexture2D stateWebsiteIcon;
        protected StateTexture2D StateWebsiteIcon
        {
            get
            {
                if (stateWebsiteIcon == null || !stateWebsiteIcon.TexturesLoadedHover)
                {
                    stateWebsiteIcon = new StateTexture2D(BaseWebsiteIcon, BaseWebsiteIcon, TintTexture(BaseWebsiteIcon, HexToColour("EEEEEE")));
                }
                return stateWebsiteIcon;
            }
            set => stateWebsiteIcon = value;
        }

        private StateTexture2D stateResetIcon;
        protected StateTexture2D StateResetIcon
        {
            get
            {
                if (stateResetIcon == null || !stateResetIcon.TexturesLoadedHover)
                {
                    stateResetIcon = new StateTexture2D(
                        TintTextureToEditorTheme(BaseResetIcon, Color.white, Color.black),
                        TintTexture(BaseResetIcon, HexToColour("979797")),
                        TintTextureToEditorTheme(BaseResetIcon, HexToColour("606162"), HexToColour("EDEEEC")));

                }
                return stateResetIcon;
            }
            set => stateResetIcon = value;
        }
        private StateTexture2D stateTransformIcon;
        protected StateTexture2D StateTransformIcon
        {
            get
            {
                if (stateTransformIcon == null || !stateTransformIcon.TexturesLoadedNoHover)
                {
                    stateTransformIcon = new StateTexture2D(
                        TintTextureToEditorTheme(BaseTransformIcon, Color.black, Color.white),
                        TintTextureToEditorTheme(BaseTransformIcon, Color.white, Color.black));
                }
                return stateTransformIcon;
            }
            set => stateTransformIcon = value;
        }
        private StateTexture2D stateGridIcon;
        protected StateTexture2D StateGridIcon
        {
            get
            {
                if (stateGridIcon == null || !stateGridIcon.TexturesLoadedHover)
                {
                    stateGridIcon = new StateTexture2D(
                        TintTextureToEditorTheme(BaseGridIcon, Color.white, Color.black),
                        TintTexture(BaseGridIcon, HexToColour("979797")),
                        TintTextureToEditorTheme(BaseGridIcon, HexToColour("606162"), HexToColour("EDEEEC")));
                }
                return stateGridIcon;
            }
            set => stateGridIcon = value;
        }
        private StateTexture2D stateClearIcon;
        protected StateTexture2D StateClearIcon
        {
            get
            {
                if (stateClearIcon == null || !stateClearIcon.TexturesLoadedHover)
                {
                    stateClearIcon = new StateTexture2D(
                        TintTextureToEditorTheme(BaseClearIcon, Color.white, Color.black),
                        TintTextureToEditorTheme(BaseClearIcon, Color.white, Color.black),
                        TintTextureToEditorTheme(BaseClearIcon, HexToColour("606162"), HexToColour("EDEEEC")));
                }
                return stateClearIcon;
            }
            set => stateClearIcon = value;
        }
        private StateTexture2D stateBackIcon;
        protected StateTexture2D StateBackIcon
        {
            get
            {
                if (stateBackIcon == null || !stateBackIcon.TexturesLoadedHover)
                {
                    stateBackIcon = new StateTexture2D(
                        TintTextureToEditorTheme(BaseBackIcon, Color.white, Color.black),
                        TintTextureToEditorTheme(BaseBackIcon, Color.white, Color.black),
                        TintTextureToEditorTheme(BaseBackIcon, HexToColour("606162"), HexToColour("EDEEEC")));
                }
                return stateBackIcon;
            }
            set => stateBackIcon = value;
        }
        private StateTexture2D stateHeartIcon;
        protected StateTexture2D StateHeartIcon
        {
            get
            {
                if (stateHeartIcon == null || !stateHeartIcon.TexturesLoadedHover)
                {
                    stateHeartIcon = new StateTexture2D(
                        EditorGUIUtility.isProSkin ? BaseFilledHeart : TintTextureWhite(BaseFilledHeart, Color.black),
                        TintTextureToEditorTheme(BaseEmptyHeart, Color.white, Color.black),
                        TintTextureToEditorTheme(BaseEmptyHeart, HexToColour("606162"), HexToColour("EDEEEC")));
                }
                return stateHeartIcon;
            }
            set => stateHeartIcon = value;
        }
        private StateTexture2D stateCollectionIcon;
        protected StateTexture2D StateCollectionIcon
        {
            get
            {
                if (stateCollectionIcon == null || !stateCollectionIcon.TexturesLoadedHover)
                {
                    stateCollectionIcon = new StateTexture2D(
                        TintTextureToEditorTheme(BaseCollectionIcon, Color.white, Color.black),
                        TintTextureToEditorTheme(BaseCollectionIcon, Color.white, Color.black),
                        TintTextureToEditorTheme(BaseCollectionIcon, HexToColour("606162"), HexToColour("EDEEEC")));
                }
                return stateCollectionIcon;
            }
            set => stateCollectionIcon = value;
        }
        private StateTexture2D stateInfoIcon;
        protected StateTexture2D StateInfoIcon
        {
            get
            {
                if (stateInfoIcon == null || !stateInfoIcon.TexturesLoadedHover)
                {
                    stateInfoIcon = new StateTexture2D(
                        TintTextureToEditorTheme(BaseInfoIcon, Color.white, Color.black),
                        TintTextureToEditorTheme(BaseInfoIcon, Color.white, Color.black),
                        TintTextureToEditorTheme(BaseInfoIcon, HexToColour("606162"), HexToColour("EDEEEC")));
                }
                return stateInfoIcon;
            }
            set => stateInfoIcon = value;
        }
        private StateTexture2D stateReportIcon;
        protected StateTexture2D StateReportIcon
        {
            get
            {
                if (stateReportIcon == null || !stateReportIcon.TexturesLoadedHover)
                {
                    stateReportIcon = new StateTexture2D(
                        TintTextureToEditorTheme(BaseReportIcon, Color.white, Color.black),
                        TintTextureToEditorTheme(BaseReportIcon, Color.white, Color.black),
                        TintTextureToEditorTheme(BaseReportIcon, HexToColour("606162"), HexToColour("EDEEEC")));
                }
                return stateReportIcon;
            }
            set => stateReportIcon = value;
        }
        #endregion State Textures
        #endregion Textures

        public static Transform objectParent;

        private bool copiedToKeyboard;
        protected Rect copiedRect;
        protected int copiedResult = 0;
        #endregion

        #region Initialization
        protected new void Awake()
        {
            base.Awake();
            AssignDefaultBehavioursFromScriptable();
        }
        protected void AssignDefaultBehavioursFromScriptable()
        {
            if (ScriptableObjectExtensions.TryGetInstance<CuratedBehaviourPreset>(out var behaviourPreset))
            {
                TransformSettings.GroundCreatureBehaviourTree = behaviourPreset.groundCreatureBehaviours.Any() ? behaviourPreset.groundCreatureBehaviours[behaviourPreset.defaultGroundCreatureIndex].behaviourTree : null;
                TransformSettings.GroundVehicleBehaviourTree = behaviourPreset.groundVehicleBehaviours.Any() ? behaviourPreset.groundVehicleBehaviours[behaviourPreset.defaultGroundVehicleIndex].behaviourTree : null;
                TransformSettings.FlyingCreatureBehaviourTree = behaviourPreset.flyingCreatureBehaviours.Any() ? behaviourPreset.flyingCreatureBehaviours[behaviourPreset.defaultFlyingCreatureIndex].behaviourTree : null;
                TransformSettings.FlyingVehicleBehaviourTree = behaviourPreset.flyingVehicleBehaviours.Any() ? behaviourPreset.flyingVehicleBehaviours[behaviourPreset.defaultFlyingVehicleIndex].behaviourTree : null;
                TransformSettings.SwimmingCreatureBehaviourTree = behaviourPreset.swimmingCreatureBehaviours.Any() ? behaviourPreset.swimmingCreatureBehaviours[behaviourPreset.defaultSwimmingCreatureIndex].behaviourTree : null;
                TransformSettings.StaticBehaviourTree = behaviourPreset.staticBehaviours.Any() ? behaviourPreset.staticBehaviours[behaviourPreset.defaultStaticIndex].behaviourTree : null;
            }
        }
        protected override void DefineCustomStyles()
        {
            base.DefineCustomStyles();
            IconStyle = new GUIStyle
            {
                normal =
                {
                    background = null
                },
                hover =
                {
                    background = null
                },
                stretchWidth = true,
                clipping = TextClipping.Overflow,
                alignment = TextAnchor.MiddleCenter
            };

            ModelNameStyle = new GUIStyle(BodyLabelStyle)
            {
                normal = SetStyleState(Color.white),
                hover = SetStyleState(Color.white),
                font = GetPoppinsFont(PoppinsStyle.Medium),
                clipping = TextClipping.Clip,
                wordWrap = false
            };

            AuthorNameStyle = new GUIStyle(BodyLabelStyle)
            {
                normal = SetStyleState(HexToColour("999999")),
                hover = SetStyleState(HexToColour("999999")),
                font = GetPoppinsFont(PoppinsStyle.Regular),
                clipping = TextClipping.Clip,
                wordWrap = false
            };

            VoteStyle = new GUIStyle(BodyLabelStyle)
            {
                normal = SetStyleState(Color.white),
                hover = SetStyleState(Color.white),
                font = GetPoppinsFont(PoppinsStyle.Medium)
            };
        }
        #endregion Initialization

        public AnythingWorld.Utilities.Data.SearchResult GetLastResult()
        {
            return lastResult;
        }
        public Texture2D GetlastThumb()
        {
            return textureThumb;
        }
        protected new void OnGUI()
        {
            base.OnGUI();

            if (!DefaultBehavioursUtility.InstanceExists())
            {
                DefaultBehavioursUtility.CreateSerializedInstance(DefaultBehaviourDictionary);
            }

            DrawWindowBanner(MaskWindowBanner());

            //if click inside window cancel drag
            if (Event.current.type == EventType.MouseDown)
            {
                if (isDragging)
                {   
                    isDragging = false;
                    SceneTextureDrawer.Instance.CancelDrag();
                }
            }

            //if drag was dropped inside window cancel drag
            if (Event.current.type == EventType.DragExited)
            {
                // Check if the drag was dropped inside the editor tab
                Rect editorTabRect = new Rect(0, 0, position.width, position.height);
                if (editorTabRect.Contains(Event.current.mousePosition))
                {
                    if (isDragging)
                    {
                        isDragging = false;
                        SceneTextureDrawer.Instance.CancelDrag();
                    }
                }
            }
            // Repaint the window to ensure the latest state is rendered
            if (GUI.changed)
            {
                Repaint();
            }
        }

        #region Editor Drawing
        protected Rect MaskWindowBanner(float bannerHeight = 88f)
        {
            return GUILayoutUtility.GetRect(position.width, bannerHeight, GUILayout.MinWidth(500));
        }

        /// <summary>
        /// Draws Anything World logo and social buttons for the Anything Creator windows.
        /// </summary>
        protected void DrawWindowBanner(Rect bannerRect)
        {
            var globeSize = 48f;

            var globeRect = new Rect(10, (bannerRect.height - globeSize) / 2f, globeSize, globeSize);
            GUI.DrawTexture(bannerRect, TintedGradientBanner);
            GUI.DrawTexture(globeRect, BlackAnythingGlobeLogo);
            var textHeight = 26;
            var textPadding = (bannerRect.height - (textHeight * 2)) / 2;

            var anythingWorldTitleStyle = new GUIStyle(EditorStyles.label) { font = GetPoppinsFont(PoppinsStyle.Bold), fontSize = textHeight, alignment = TextAnchor.MiddleLeft, normal = SetStyleState(Color.black), hover = SetStyleState(Color.black) };
            var anythingWorldContent = new GUIContent("ANYTHING WORLD");
            var anythingWorldTitleRect = new Rect(globeRect.xMax + 10, textPadding, anythingWorldTitleStyle.CalcSize(anythingWorldContent).x, textHeight);

            var windowTitleStyle = new GUIStyle(EditorStyles.label) { font = GetPoppinsFont(PoppinsStyle.Bold), fontSize = textHeight, alignment = TextAnchor.MiddleLeft, normal = SetStyleState(Color.white), hover = SetStyleState(Color.white) };
            var windowTitleContent = new GUIContent(windowTitle);
            var windowTitleRect = new Rect(globeRect.xMax + 10, anythingWorldTitleRect.yMax, windowTitleStyle.CalcSize(windowTitleContent).x, textHeight);

            GUI.Label(anythingWorldTitleRect, anythingWorldContent, anythingWorldTitleStyle);
            GUI.Label(windowTitleRect, windowTitleContent, windowTitleStyle);

            var iconSize = 20f;
            var iconPadding = 4f;
            var iconYMargin = (bannerRect.height - ((iconSize * 2) + (iconPadding * 1))) / 2;
            var iconXMargin = 8f;

            var iconsXPos = bannerRect.xMax - iconSize - iconXMargin;
            var iconsYPos = bannerRect.yMin + iconYMargin;

            var versionContent = new GUIContent(AnythingSettings.PackageVersion);
            var versionStyle = new GUIStyle(EditorStyles.label) { font = GetPoppinsFont(PoppinsStyle.Medium), fontSize = 9, alignment = TextAnchor.MiddleRight, normal = SetStyleState(Color.white), hover = SetStyleState(Color.white) };
            var versionRectSize = versionStyle.CalcSize(versionContent);
            var versionRect = new Rect(windowTitleRect.xMax + 5, windowTitleRect.center.y - versionRectSize.y / 2, versionRectSize.x + versionRectSize.y, versionRectSize.y);

            var versionLeftEdgeRect = new Rect(versionRect.xMin, versionRect.yMin, versionRectSize.y / 2, versionRectSize.y);
            var versionRightEdgeRect = new Rect(versionRect.xMax - (versionRectSize.y / 2), versionRect.yMin, versionRectSize.y / 2, versionRectSize.y);
            var versionMainRect = new Rect(versionLeftEdgeRect.xMax, versionRect.y, versionRect.width - versionLeftEdgeRect.width - versionRightEdgeRect.width, versionRectSize.y);

            GUI.DrawTexture(versionLeftEdgeRect, BaseLabelBackdropLeft);
            GUI.DrawTexture(versionMainRect, BaseLabelBackdropMiddle);
            GUI.DrawTexture(versionRightEdgeRect, BaseLabelBackdropRight);
            GUI.Label(versionMainRect, versionContent, versionStyle);

            var discordIconRect = new Rect(iconsXPos, iconsYPos, iconSize, iconSize);
            var websiteIconRect = new Rect(iconsXPos, discordIconRect.yMax + iconPadding, iconSize, iconSize);

            if (GUI.Button(discordIconRect, "", new GUIStyle(IconStyle) { normal = SetStyleState(StateDiscordIcon.activeTexture), hover = SetStyleState(StateDiscordIcon.hoverTexture) })) System.Diagnostics.Process.Start("https://discord.gg/anythingworld");
            if (GUI.Button(websiteIconRect, "", new GUIStyle(IconStyle) { normal = SetStyleState(StateWebsiteIcon.activeTexture), hover = SetStyleState(StateWebsiteIcon.hoverTexture) })) System.Diagnostics.Process.Start("https://www.anything.world/");
        }

        protected void DrawLoading(Rect miscRect)
        {
            var thisTime = EditorApplication.timeSinceStartup;
            var workArea = GUILayoutUtility.GetRect(position.width, position.height - (miscRect.y + miscRect.height));
            var logoSize = workArea.height / 4;
            var spinningRect = new Rect((workArea.width / 2) - (logoSize / 2), workArea.y + (workArea.height / 2) - (logoSize / 2), logoSize, logoSize);
            var logoRect = new Rect(spinningRect.x + (spinningRect.width / 6), spinningRect.y + (spinningRect.height / 6), spinningRect.width * (2f / 3f), spinningRect.height * (2f / 3f));
            var dt = EditorApplication.timeSinceStartup - lastEditorTime;
            var matrixBack = GUI.matrix;
            searchRingAngle += 75f * (float)dt;
            GUIUtility.RotateAroundPivot(searchRingAngle, spinningRect.center);
            GUI.DrawTexture(spinningRect, BaseLoadingCircle);
            GUI.matrix = matrixBack;
            GUI.DrawTexture(logoRect, EditorGUIUtility.isProSkin ? BaseAnythingGlobeLogo : BlackAnythingGlobeLogo);
            lastEditorTime = thisTime;
        }

        protected void DrawLoadingSmall(Rect workRect, float iconScalar = 1f, bool black = false)
        {
            var thisTime = EditorApplication.timeSinceStartup;

            var iconSize = workRect.height * iconScalar;
            var spinningRect = new Rect(workRect.center.x - (iconSize / 2), workRect.center.y - (iconSize / 2), iconSize, iconSize);
            var dt = EditorApplication.timeSinceStartup - lastEditorTime;
            var matrixBack = GUI.matrix;
            searchRingAngle += 75f * (float)dt;
            GUIUtility.RotateAroundPivot(searchRingAngle, spinningRect.center);
            GUI.DrawTexture(spinningRect, black ? BlackLoadingIconSmall : BaseLoadingIconSmall);
            GUI.matrix = matrixBack;
            lastEditorTime = thisTime;
        }

        protected void DrawError(Rect rect)
        {
            var padding = 20f;
            var xMargin = 20f;
            var yMargin = 30f;

            var backdropWidth = position.width * 0.8f;
            var backdropHeight = yMargin * 2;

            var iconSize = new Vector2(64f, 64f);
            backdropHeight += iconSize.y;
            backdropHeight += padding;

            var errorStyle = new GUIStyle() { font = GetPoppinsFont(PoppinsStyle.Regular), fontSize = 16, normal = SetStyleState(Color.white), hover = SetStyleState(Color.white), wordWrap = true, alignment = TextAnchor.MiddleCenter };
            var errorContent = new GUIContent(searchModeFailReason);
            var errorContentSize = new Vector2(backdropWidth - (xMargin * 2), errorStyle.CalcHeight(errorContent, backdropWidth - (xMargin * 2)));
            backdropHeight += errorContentSize.y;
            backdropHeight += padding;

            var buttonSize = new Vector2(backdropWidth * 0.5f, 40f);
            backdropHeight += buttonSize.y;

            var backdropRect = DrawSquareInSquare(new Vector2(position.width / 2, rect.y + (position.height - (rect.y + rect.height)) / 2), position.width * 0.8f, backdropHeight, 2f);

            var iconRect = new Rect(backdropRect.center.x - (iconSize.x / 2), backdropRect.y + yMargin, iconSize.x, iconSize.y);
            var errorRect = new Rect(backdropRect.center.x - (errorContentSize.x / 2), iconRect.yMax + padding, errorContentSize.x, errorContentSize.y);
            var buttonRect = new Rect(backdropRect.center.x - (buttonSize.x / 2), errorRect.yMax + padding, buttonSize.x, buttonSize.y);

            GUI.DrawTexture(iconRect, BaseAnythingGlobeErrorLogo);
            GUI.Label(errorRect, searchModeFailReason, errorStyle);
            if (DrawRoundedButton(buttonRect, new GUIContent("Reset Creator"), 16))
            {
                ResetAnythingWorld(ResetMode.Creator);
            }
        }

        protected void DrawSettingsIcons(Rect settingsRect)
        {
            
            GUILayout.BeginHorizontal();
            bool transformWindowOpen = HasOpenInstances<TransformSettingsEditor>();
            var makeOptionsContent = new GUIContent("Transform Settings", transformWindowOpen ? StateTransformIcon.activeTexture : StateTransformIcon.inactiveTexture);
            var makeOptionsRect = new Rect(settingsRect.x, settingsRect.y, Mathf.Max(settingsRect.width / 4, 160f), settingsRect.height);
            if (DrawSquareButton(makeOptionsRect, makeOptionsContent, transformWindowOpen, 13))
            {
                if (transformWindowOpen)
                {
                    CloseWindowIfOpen<TransformSettingsEditor>();
                }
                else
                {
                    TransformSettingsEditor.Initialize();
                }
            }

            GUILayout.FlexibleSpace();

            var iconSize = 16;
            var settingsIconsRectSize = new Vector2(settingsRect.width / 4, iconSize * 2);
            var settingsIconsRect = new Rect(settingsRect.xMax - settingsIconsRectSize.x, settingsRect.y, settingsIconsRectSize.x, settingsIconsRectSize.y);//GUILayoutUtility.GetRect((Position.width / 3) - DropdownStyle.margin.horizontal - 8, iconSize * 2);
            GUILayout.EndHorizontal();

            var marginY = (settingsIconsRect.height - iconSize) / 2;

            var resetIconRect = new Rect(settingsIconsRect.xMax - iconSize, settingsIconsRect.y + marginY, iconSize, iconSize);

            //Dropdown to Reset Grid, Reset Creator, and Reset All
            if (GUI.Button(resetIconRect, "", new GUIStyle(IconStyle) { normal = SetStyleState(StateResetIcon.activeTexture), hover = SetStyleState(StateResetIcon.hoverTexture) }))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Reset Scene"), false, () => SetupResetEditorWindow(ResetMode.Scene));
                menu.AddItem(new GUIContent("Reset Creator"), false, () => SetupResetEditorWindow(ResetMode.Creator));
                menu.AddItem(new GUIContent("Reset All"), false, () => SetupResetEditorWindow(ResetMode.All));
                menu.DropDown(resetIconRect);
            }
            if (makeOptionsRect.Contains(Event.current.mousePosition))
            {
                if (isDragging && Event.current.type == EventType.MouseUp)
                {
                    isDragging = false;
                    SceneTextureDrawer.Instance.CancelDrag();
                }
            }
        }

        protected void DrawFilters(bool sortByLikedIncluded = true)
        {
            List<DropdownOption> localSortingFilter = SortingFilter.ToList();
            if (!sortByLikedIncluded) localSortingFilter.RemoveAt(sortingFilter.Length - 1);

            GUILayout.Label("FILTER", new GUIStyle(BodyLabelStyle) { alignment = TextAnchor.MiddleLeft, font = GetPoppinsFont(PoppinsStyle.Bold), normal = SetStyleState(Color.white), hover = SetStyleState(Color.white), margin = new RectOffset(10, 10, 0, 0), fontSize = 12 });
            GUILayout.BeginHorizontal();
            var categoryRect = GUILayoutUtility.GetRect(position.width / 3, 40);
            DrawDropdown(categoryRect, CategoryFilter, currentCategory, "CATEGORY");
            var animationRect = GUILayoutUtility.GetRect(position.width / 3, 40);
            DrawDropdown(animationRect, AnimationFilter, currentAnimationFilter, "ANIMATED");
            var sortingRect = GUILayoutUtility.GetRect(position.width / 3, 40);
            DrawDropdown(sortingRect, localSortingFilter.ToArray(), currentSortingMethod, "SORT BY");
            GUILayout.EndHorizontal();
            //block the drag if the mouse is over the filters
            if (categoryRect.Contains(Event.current.mousePosition) || animationRect.Contains(Event.current.mousePosition) || sortingRect.Contains(Event.current.mousePosition))
            {
                if (isDragging && Event.current.type == EventType.MouseUp)
                {
                    isDragging = false;
                    SceneTextureDrawer.Instance.CancelDrag();
                }
            }
        }
        protected void DrawBrowserCard(List<SearchResult> resultArray, float columnCoord, float rowCoord, float buttonWidth, float buttonHeight, int searchIndex, float resultScaleMultiplier)
        {
            try
            {
                Event e = Event.current;
                // Set result data
                var result = resultArray[searchIndex];
                var displayThumbnail = result.Thumbnail;

                var modelName = new GUIContent(result.DisplayName, result.name);
                var authorName = new GUIContent(result.data.author, result.data.author);
                var cardRect = new Rect(columnCoord, rowCoord, buttonWidth, buttonHeight);

                // Initialize padding and sizing
                var iconSizeY = buttonHeight / 12;
                var iconSizeX = iconSizeY;

                var infoPaddingX = iconSizeX / 3f;
                var infoPaddingY = iconSizeY / 3f;
                
                //Draw elements
                GUI.DrawTexture(cardRect, TintedCardFrame, ScaleMode.ScaleToFit);
                
                var thumbnailRatio = (float)BaseCardThumbnailBackdrops[0].height / (float)BaseCardThumbnailBackdrops[0].width;
                var thumbnailBackdropRect = new Rect(cardRect.x, cardRect.y, buttonWidth, buttonWidth * thumbnailRatio);
                
                GUI.DrawTexture(thumbnailBackdropRect, BaseCardThumbnailBackdrops[searchIndex % BaseCardThumbnailBackdrops.Length], ScaleMode.ScaleToFit);
                
                if (displayThumbnail != null)
                {
                    GUI.DrawTexture(thumbnailBackdropRect, displayThumbnail, ScaleMode.ScaleAndCrop);
                }
                else
                {
                    DrawLoadingSmall(thumbnailBackdropRect, 0.25f, true);
                }
                
                var infoRect = new Rect(thumbnailBackdropRect.x, thumbnailBackdropRect.yMax, buttonWidth, cardRect.height - thumbnailBackdropRect.height);

                DrawCardVoteButton(result, ref infoRect, iconSizeX, iconSizeY, infoPaddingX, infoPaddingY, out var voteRect);
                DrawCardVoteCountLabel(infoPaddingX, voteRect, result.data.voteScore, resultScaleMultiplier);

                DrawCardInfoIcon(result, ref infoRect, iconSizeX, iconSizeY, infoPaddingX, infoPaddingY, out var detailRect);
                DrawCardListIcon(result, detailRect, iconSizeX, iconSizeY, infoPaddingX);

                DrawCardModelNameLabel(modelName, ref infoRect, infoPaddingX, infoPaddingY * 0.5f, out var modelNameLabelRect, resultScaleMultiplier);
                DrawCardAuthorLabel(authorName, ref infoRect, infoPaddingX, infoPaddingY * 0.75f, modelNameLabelRect, resultScaleMultiplier);
                
                if (result.isAnimated) DrawCardAnimationStatusIcon(thumbnailBackdropRect, iconSizeX, iconSizeY, infoPaddingX, infoPaddingY);

                if (cardRect.Contains(e.mousePosition))
                {
                    GUI.DrawTexture(cardRect, BaseObjectSelectionTint, ScaleMode.ScaleToFit);
                    if (e.button == 0 && e.isMouse)
                    {
                        GUI.DrawTexture(cardRect, BaseObjectSelectionTint, ScaleMode.ScaleToFit);
                        if (Event.current.type == EventType.MouseDown)
                        {
                            buttonPressed = true;
                            if (!SceneTextureDrawer.Instance.IsEnabled())
                            {
                                SceneTextureDrawer.Instance.Enable();
                            }
                        }
                        if (Event.current.type == EventType.MouseUp)
                        {
                            buttonPressed = false;
                            if (TransformSettings.ClickInPlacementLocation)
                            {
                                textureThumb = result.Thumbnail;
                                //send the result to the scene texture drawer
                                SceneTextureDrawer.Instance.SetCallBack(result.Thumbnail, MakeResult, result, ref isDragging);
                            }
                            else
                            {
                                MakeResult(result);
                            }
                        }
                        if (buttonPressed && Event.current.type == EventType.MouseDrag)
                        {
                            textureThumb = result.Thumbnail;
                            SceneTextureDrawer.Instance.SetCallBack(result.Thumbnail, MakeResult, result, ref isDragging);
                            isDragging = true;
                            DragAndDrop.PrepareStartDrag();
                            DragAndDrop.SetGenericData("Object", this);
                            DragAndDrop.StartDrag("Dragging an Object");
                        }
                        Repaint();
                    }
                    else if (e.button == 1 && e.isMouse && isDragging)
                    {
                        SceneTextureDrawer.Instance.CancelDrag();
                        isDragging = false;
                    }
                }

                if (isDragging && textureThumb)
                {
                    Vector2 texsize = new Vector2(textureThumb.width, textureThumb.height) * 0.8f;
                    GUI.DrawTexture(new Rect(e.mousePosition.x - texsize.x / 2, e.mousePosition.y - texsize.y / 2, texsize.x, texsize.y), textureThumb);
                    Repaint();
                }

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        protected void MakeResult(SearchResult result, bool forceSerialize = false)
        {
            var inputParams = new RequestParams();
            
            inputParams
                .SetPosition(TransformSettings.PositionField)
                .SetRotation(TransformSettings.RotationField)
                .SetScaleMultiplier(TransformSettings.ScaleField)
                .SetParent(objectParent)
                .SetPlaceOnGrid(TransformSettings.PlaceOnGrid)
                .SetPlaceOnGround(TransformSettings.PlaceOnGround)
                .SetUseGridArea(TransformSettings.GridAreaEnabled)
                .SetAddRigidbody(TransformSettings.AddRigidbody)
                .SetAddCollider(TransformSettings.AddCollider)
                .SetDefaultBehaviourPreset(DefaultBehavioursUtility.CreateNewTemporaryInstance(DefaultBehaviourDictionary))
                .SetAnimateModel(TransformSettings.AnimateModel)
                .SetModelCaching(TransformSettings.CacheModel)
                .SetSerializeAssets(TransformSettings.SerializeAsset || forceSerialize);

            GameObject gameObject;
            
            // if the result is processed, make the object with the json
            if (result.IsProcessedResult)
            {
                gameObject = AnythingMaker.Make(result.json, inputParams);
            }
            else if (result.isLandingPageSearch && result.data.model.rig.animations != null)
            {
                gameObject = AnythingMaker.MakeByJson(result.data, inputParams);
            }
            else
            {
                gameObject = AnythingMaker.MakeById(result.data._id, inputParams);
            }
            if (TransformSettings.FollowCam)
            {
                EditorCoroutineUtility.StartCoroutine(WaitAndLook(gameObject), this);
            }
            if (TransformSettings.Repositioning && !TransformSettings.PlaceOnGrid)
            {
                EditorCoroutineUtility.StartCoroutine(WaitAndReposition(gameObject), this);
            }
            EditorCoroutineUtility.StartCoroutine(WaitToLoad(gameObject), this);
        }

        //This is the method that is called when the user clicks or drag on the scene
        protected GameObject MakeResult(SearchResult result, Vector3 position, bool forceSerialize = false)
        {
            DefaultBehavioursUtility.CreateSerializedInstance(DefaultBehaviourDictionary);

            if (result == null)
            {
                isDragging = false;
                if (MyWorldEditor.instance != null)
                    MyWorldEditor.Instance.ResetDrag();
                return null;
            }
            
            var inputParams = new RequestParams();
            inputParams
                .SetPosition(position)
                .SetRotation(TransformSettings.RotationField)
                .SetScaleMultiplier(TransformSettings.ScaleField)
                .SetParent(objectParent)
                .SetPlaceOnGrid(false)
                .SetUseGridArea(false)
                .SetPlaceOnGround(TransformSettings.PlaceOnGround)
                .SetAddRigidbody(TransformSettings.AddRigidbody)
                .SetAddCollider(TransformSettings.AddCollider)
                .SetDefaultBehaviourPreset(DefaultBehavioursUtility.CreateNewTemporaryInstance(DefaultBehaviourDictionary))
                .SetAnimateModel(TransformSettings.AnimateModel)
                .SetModelCaching(TransformSettings.CacheModel)
                .SetSerializeAssets(TransformSettings.SerializeAsset || forceSerialize);
            
            // set the position of the object and using in ModelPositioning
            TransformSettings.DragPosition = position;
            
            GameObject gameObject;
            
            // if the result is processed, make the object with the json
            if (result.IsProcessedResult)
            {
                gameObject = AnythingMaker.Make(result.json, inputParams);
            }
            else if(result.isLandingPageSearch && result.data.model.rig.animations != null)
            {
                gameObject = AnythingMaker.MakeByJson(result.data, inputParams);
            }
            else
            {
                gameObject = AnythingMaker.MakeById(result.data._id, inputParams);
            }
           
            isDragging = false;
            if (MyWorldEditor.instance != null)
                MyWorldEditor.Instance.ResetDrag();
            return gameObject;
        }

        public GameObject ExternalMakeResult(SearchResult result, Vector3 position, bool forceSerialize = false)
        {
            return MakeResult(result, position, forceSerialize);
        }

        // Wait for the object to be created
        IEnumerator WaitAndLook(GameObject ob)
        {
            while (ob.transform.childCount < 1)
            {
                yield return new WaitForSeconds(0.5f);
            }
            //look to the object
            SceneView.lastActiveSceneView.LookAt(ob.transform.position);
            SceneView.lastActiveSceneView.Repaint();

        }
        
        // Wait for the object to be created
        IEnumerator WaitAndReposition(GameObject gameObject)
        {
            while (gameObject.transform.childCount < 1)
            {
                yield return new WaitForSeconds(0.5f);
            }

            // check if there's something colliding around the object
            if (CheckCollision(gameObject))
            {
                // if there's something colliding, move the object to the nearest free position
                gameObject.transform.position = GetFreePosition(gameObject.transform.position);
                // place the object in ground
                if (TransformSettings.PlaceOnGround)
                {
                    // check if there ground under the object
                    RaycastHit hit;
                    if (Physics.Raycast(gameObject.transform.position + Vector3.up * 100, Vector3.down, out hit, 200f))
                    {
                        // put the object in the ground respecting bounding box
                        gameObject.transform.position = new Vector3(gameObject.transform.position.x, 
                            hit.point.y + gameObject.GetComponentInChildren<Renderer>().bounds.extents.y, 
                            gameObject.transform.position.z);
                    }
                }
            }
        }

        // Wait for the object to be created
        IEnumerator WaitToLoad(GameObject ob)
        {
            if (TransformSettings.LoadingMessage)
            {
                EditorLoadingIcon.Instance.ShowToastyMessage("Loading", this);
            }
            else
            {
                yield return null;
            }

            if (ob != null)
            {
                yield return null;
            }

            String dots = "";
            while (ob != null && ob.transform.childCount < 1)
            {
                yield return new WaitForSecondsRealtime(timeToWaitIcon);
                EditorLoadingIcon.Instance.ShowToastyMessage("Loading" + dots, this, timeToWaitIcon);
                dots += ".";
                if (dots.Length > 3)
                {
                    dots = "";
                }
            }

            if(ob != null)
            {
                EditorLoadingIcon.Instance.ShowToastyMessage("Model added!", this);
            }
            else
            {
                EditorLoadingIcon.Instance.ShowToastyMessage("Error adding model!", this);
            }
        }

        async UniTask WaitAndPutOnGround(GameObject ob)
        {
            while (ob.transform.childCount < 1)
            {
                await UniTask.WaitForSeconds(0.5f);
            }
            
            // get the childs of the object
            List<Transform> childs = new List<Transform>();
            foreach (Transform child in ob.transform)
            {
                childs.Add(child);
            }
            
            // put the object in the ground
            // check if there ground under the object
            RaycastHit hit;
            if (Physics.Raycast(ob.transform.position, Vector3.down, out hit, 100f))
            {

                // put the object in the ground respecting bounding box
                ob.transform.localPosition = new Vector3(ob.transform.localPosition.x, 
                    hit.point.y + ob.GetComponentInChildren<Renderer>().bounds.extents.y, ob.transform.localPosition.z);
                // put the childs in absolute center
                for (int i = 0; i < childs.Count; i++)
                {
                    childs[i].localPosition = Vector3.zero;
                }

            }
        }
        
        // wait for the object to be created and force the position
        IEnumerator WaitAndForcePosition(GameObject ob, Vector3 pos)
        {
            while (ob.transform.childCount < 1)
            {
                yield return new WaitForSeconds(0.5f);
            }
            //put the object in the position
            ob.transform.root.position = pos;
        }

        // check if there's something colliding around the object
        bool CheckCollision(GameObject ob)
        {
            return Physics.CheckSphere(ob.transform.position, 0.5f);
        }
        
        // get the nearest free position
        Vector3 GetFreePosition(Vector3 position)
        {
            Vector3 freePosition = position;
            Vector3[] positions = new Vector3[8];
            positions[0] = new Vector3(position.x + 1, position.y, position.z);
            positions[1] = new Vector3(position.x - 1, position.y, position.z);
            positions[2] = new Vector3(position.x, position.y + 1, position.z);
            positions[3] = new Vector3(position.x, position.y - 1, position.z);
            positions[4] = new Vector3(position.x, position.y, position.z + 1);
            positions[5] = new Vector3(position.x, position.y, position.z - 1);
            positions[6] = new Vector3(position.x + 1, position.y + 1, position.z);
            positions[7] = new Vector3(position.x - 1, position.y - 1, position.z);

            foreach (Vector3 pos in positions)
            {
                if (!Physics.CheckSphere(pos, 0.5f))
                {
                    freePosition = pos;
                    break;
                }
            }
            return freePosition;
        }

        #region Browser Card Draw Methods
        protected void DrawCardVoteButton(SearchResult result, ref Rect infoBackdropRect, float voteIconSizeX, 
            float voteIconSizeY, float infoPaddingX, float infoPaddingY, out Rect voteRect)
        {
            //if the result is processed, don't draw the vote button
            if (result.IsProcessedResult)
            {
                voteRect = Rect.zero;
                return;
            }
            voteRect = new Rect(infoBackdropRect.x + infoPaddingX, infoBackdropRect.yMax - infoPaddingY - voteIconSizeY, 
                voteIconSizeX, voteIconSizeY);
            if (GUI.Button(voteRect, new GUIContent((string)null, "Like Model"), 
                    new GUIStyle(IconStyle) { normal = SetStyleState(result.data.userVote == "upvote" ? 
                        StateHeartIcon.activeTexture : StateHeartIcon.inactiveTexture), 
                        hover = SetStyleState(StateHeartIcon.hoverTexture) }))
            {
                UserVoteProcessor.FlipUserVote(result, EditorNetworkErrorHandler.HandleError, this);
            }
        }

        protected void DrawCardVoteCountLabel(float infoPaddingX, Rect voteRect, int voteCount, float scaleMultiplier)
        {
            var voteStyle = new GUIStyle(VoteStyle) { fontSize = (int)(12 * scaleMultiplier) };
            var voteContent = new GUIContent(TruncateNumber(voteCount));
            var voteLabelWidth = voteStyle.CalcSize(voteContent).x;
            var voteLabelRect = new Rect(voteRect.xMax + (infoPaddingX / 2), voteRect.y, voteLabelWidth, voteRect.height);
            GUI.Label(voteLabelRect, voteContent, voteStyle);
        }

        protected void DrawCardAuthorIconBackground(ref Rect infoBackdropRect, ref Rect thumbnailBackdropRect, 
            float infoPaddingX, float infoPaddingY, out Rect userIconRect, float scaleMultiplier)
        {
            var userIconSize = BaseUserDefaultProfile.width / 2.5f * scaleMultiplier;
            userIconRect = new Rect(infoBackdropRect.x + infoPaddingX, thumbnailBackdropRect.yMax + infoPaddingY / 1.5f, 
                userIconSize, userIconSize);
            GUI.DrawTexture(userIconRect, BaseUserDefaultProfile);
        }


        private void DrawCardListIcon(SearchResult result, Rect infoRect, float iconSizeX, float iconSizeY, 
            float infoPaddingX)
        {
            // if the result is processed, don't draw the list button
            if (result.IsProcessedResult)
            {

                return;
            }
            var listRect = new Rect(infoRect.x - infoPaddingX - iconSizeX, infoRect.y, iconSizeX, iconSizeY);
            if (GUI.Button(listRect, new GUIContent((string)null, "Add to Collection"), 
                    new GUIStyle(IconStyle) { normal = SetStyleState(StateCollectionIcon.activeTexture), 
                        hover = SetStyleState(StateCollectionIcon.hoverTexture) }))
            {
                CollectionProcessor.GetCollectionNamesAsync(CollectionReceived, EditorNetworkErrorHandler.HandleError, this).Forget();
                void CollectionReceived(string[] results)
                {
                    existingCollections = results;
                    AnythingSubwindow.OpenWindow("Add to Collection", 
                        new Vector2(Mathf.Max(position.width - 80, 450), 200), DrawCollectionWindow, position, result);
                }
            }
        }

        protected void DrawCardInfoIcon(SearchResult result, ref Rect infoBackdropRect, float iconSizeX, 
            float iconSizeY, float infoPaddingX, float infoPaddingY, out Rect infoRect)
        {
            infoRect = new Rect(infoBackdropRect.xMax - infoPaddingX - iconSizeX, 
                infoBackdropRect.yMax - infoPaddingY - iconSizeY, iconSizeX, iconSizeY);
            if (GUI.Button(infoRect, new GUIContent((string)null, "More Details"), 
                    new GUIStyle(IconStyle) { normal = SetStyleState(StateInfoIcon.activeTexture), 
                        hover = SetStyleState(StateInfoIcon.hoverTexture) }))
            {
                selectedResult = result;
            }
        }

        protected void DrawCardAnimationStatusIcon(Rect thumbnailBackdropRect, float iconSizeX, float iconSizeY, 
            float infoPaddingX, float infoPaddingY)
        {
            var animatedRect = new Rect(thumbnailBackdropRect.x + infoPaddingX, thumbnailBackdropRect.y + infoPaddingY, 
                iconSizeX, iconSizeY);
            var iconSize = new Vector2(iconSizeX, iconSizeY);
            EditorGUIUtility.SetIconSize(iconSize);
            GUI.Label(animatedRect, new GUIContent(BaseAnimatedIcon, "This Model is Animated"), GUIStyle.none);
        }

        protected void DrawCardModelNameLabel(GUIContent modelName, ref Rect infoBackdropRect, float infoPaddingX, 
            float infoPaddingY, out Rect modelNameLabelRect, float scaleMultiplier)
        {
            var modelNameStyle = new GUIStyle(ModelNameStyle) { fontSize = (int)(12 * scaleMultiplier) };
            var modelNameXPos = infoBackdropRect.x + infoPaddingX;
            var modelNameSize = modelNameStyle.CalcSize(modelName);
            var modelNameLabelWidth = Mathf.Min(modelNameSize.x, infoBackdropRect.xMax - modelNameXPos - infoPaddingX);
            modelNameLabelRect = new Rect(modelNameXPos, infoBackdropRect.y + infoPaddingY, modelNameLabelWidth, 
                modelNameSize.y);
            GUI.Label(modelNameLabelRect, modelName, modelNameStyle);
        }

        protected void DrawCardAuthorLabel(GUIContent authorName, ref Rect infoBackdropRect, float infoPaddingX, 
            float infoPaddingY, Rect modelNameLabelRect, float scaleMultiplier)
        {
            var authorStyle = new GUIStyle(AuthorNameStyle) { fontSize = (int)(8 * scaleMultiplier) };
            var authorNameXPos = modelNameLabelRect.x;
            var authorNameSize = authorStyle.CalcSize(authorName);
            var authorNameLabelWidth = Mathf.Min(authorNameSize.x, infoBackdropRect.xMax - authorNameXPos - infoPaddingX);
            var authorNameLabelRect = new Rect(authorNameXPos, modelNameLabelRect.yMax - infoPaddingY, 
                authorNameLabelWidth, authorNameSize.y);
            GUI.Label(authorNameLabelRect, authorName, authorStyle);
        }
        #endregion
        protected void DrawGrid<T>(List<T> results, int cellCount, float cellWidth, float cellHeight, 
            Action<List<T>, float, float, float, float, int, float> drawCellFunction, ref Vector2 newScrollPosition, 
            float scaleMultiplier = 1f)
        {
            if (cellCount == 0) return;

            var internalMultiplier = 1.5f;
            var buttonWidth = cellWidth * internalMultiplier * scaleMultiplier;
            var buttonHeight = cellHeight * internalMultiplier * scaleMultiplier;
            var aspectRatio = cellHeight / cellWidth;

            var verticalMargin = 5 * internalMultiplier;
            var horizontalMargin = 5 * internalMultiplier;
            float scrollBarAllowance = 6;
            var buttonWidthWithMargin = buttonWidth + horizontalMargin;
            var resultsPerLine = Mathf.Floor((position.width - horizontalMargin) / buttonWidthWithMargin);
            if (resultsPerLine == 0)
            {
                resultsPerLine = 1;
                var scalingFix = scaleMultiplier;
                if (buttonWidth > position.width)
                {
                    scalingFix = (position.width / cellWidth) / internalMultiplier;
                    buttonWidth = position.width;
                    buttonHeight = buttonWidth * aspectRatio;
                    buttonWidthWithMargin = buttonWidth + horizontalMargin;
                }
                scaleMultiplier = scalingFix;
            }
            var rows = (int)Math.Ceiling(cellCount / resultsPerLine);
            var actualBlockWidth = (resultsPerLine * buttonWidthWithMargin) + horizontalMargin;
            var outerRemainder = position.width - actualBlockWidth;
            var remainderMargin = outerRemainder / 2;

            var lastRect = GUILayoutUtility.GetLastRect();
            var gridArea = new Rect(0, lastRect.yMax, position.width + scrollBarAllowance, 
                buttonHeight * rows + verticalMargin * rows);
            var view = new Rect(0, lastRect.yMax, position.width, position.height - lastRect.yMax);
            newScrollPosition = GUI.BeginScrollView(view, newScrollPosition, gridArea, false, false, GUIStyle.none, 
                GUI.skin.verticalScrollbar);

            if (copiedToKeyboard)
            {
                if (!copiedRect.Contains(Event.current.mousePosition))
                {
                    copiedToKeyboard = false;
                }
            }
            var scrollViewRect = new Rect(new Vector2(view.x, view.y + newScrollPosition.y), view.size);
            
            // Iterate through rows and draw
            for (var yPos = 0; yPos < rows; yPos++)
            {
                var rowCoord = view.yMin + yPos * buttonHeight + verticalMargin * yPos;

                if (rowCoord > scrollViewRect.yMax) continue;
                if (rowCoord + buttonHeight < scrollViewRect.yMin) continue;

                for (var xPos = 0; xPos < resultsPerLine; xPos++)
                {
                    var columnCoord = (xPos * buttonWidthWithMargin) + horizontalMargin + 
                                      (remainderMargin - scrollBarAllowance);
                    var index = (yPos * (int)resultsPerLine) + xPos;

                    if (results.Count > index)
                    {
                        drawCellFunction(results, columnCoord, rowCoord, buttonWidth, buttonHeight, 
                            yPos * Mathf.FloorToInt(resultsPerLine) + xPos, scaleMultiplier);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            GUI.EndScrollView();
        }
        
        protected void DrawGrid<T>(List<T> results, Rect rect, int cellCount, float cellWidth, 
            float cellHeight, Action<List<T>, float, float, float, float, int, float> drawCellFunction, 
            ref Vector2 newScrollPosition, float scaleMultiplier = 1f)
        {
            if (cellCount == 0) return;

            var internalMultiplier = 1.5f;
            var buttonWidth = cellWidth * internalMultiplier * scaleMultiplier;
            var buttonHeight = cellHeight * internalMultiplier * scaleMultiplier;
            var aspectRatio = cellHeight / cellWidth;

            var verticalMargin = 5 * internalMultiplier;
            var horizontalMargin = 5 * internalMultiplier;
            float scrollBarAllowance = 6;
            var buttonWidthWithMargin = buttonWidth + horizontalMargin;
            var resultsPerLine = Mathf.Floor((rect.width - horizontalMargin) / buttonWidthWithMargin);
            if (resultsPerLine == 0)
            {
                resultsPerLine = 1;
                var scalingFix = scaleMultiplier;
                if (buttonWidth > rect.width)
                {
                    scalingFix = (rect.width / cellWidth) / internalMultiplier;
                    buttonWidth = rect.width;
                    buttonHeight = buttonWidth * aspectRatio;
                    buttonWidthWithMargin = buttonWidth + horizontalMargin;
                }
                scaleMultiplier = scalingFix;
            }
            var rows = (int)Math.Ceiling(cellCount / resultsPerLine);
            var actualBlockWidth = (resultsPerLine * buttonWidthWithMargin) + horizontalMargin;
            var outerRemainder = rect.width - actualBlockWidth;
            var remainderMargin = outerRemainder / 2;

            var gridArea = new Rect(rect.x, rect.y, rect.width + scrollBarAllowance, buttonHeight * rows + 
                verticalMargin * rows);
            var view = rect;
            newScrollPosition = GUI.BeginScrollView(view, newScrollPosition, gridArea, false, false, GUIStyle.none, 
                GUI.skin.verticalScrollbar);

            if (copiedToKeyboard)
            {
                if (!copiedRect.Contains(Event.current.mousePosition))
                {
                    copiedToKeyboard = false;
                }
            }
            var scrollViewRect = new Rect(new Vector2(view.x, view.y + newScrollPosition.y), view.size);
            
            // Iterate through rows and draw
            for (var yPos = 0; yPos < rows; yPos++)
            {
                var rowCoord = view.yMin + (yPos * buttonHeight) + (verticalMargin * yPos);

                if (rowCoord > scrollViewRect.yMax) continue;
                if (rowCoord + buttonHeight < scrollViewRect.yMin) continue;

                for (var xPos = 0; xPos < resultsPerLine; xPos++)
                {
                    var columnCoord = view.xMin + (xPos * buttonWidthWithMargin) + horizontalMargin +
                                      (remainderMargin - scrollBarAllowance);
                    var index = (yPos * (int)resultsPerLine) + xPos;

                    if (results.Count > index)
                    {
                        drawCellFunction(results, columnCoord, rowCoord, buttonWidth, buttonHeight, 
                            yPos * Mathf.FloorToInt(resultsPerLine) + xPos, scaleMultiplier);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            GUI.EndScrollView();
        }
        
        protected void DrawGrid<T>(Dictionary<string, T> results, Rect rect, int cellCount, float cellWidth, 
            float cellHeight, Action<Dictionary<string, T>, float, float, float, float, int, float> drawCellFunction, 
            ref Vector2 newScrollPosition, float scaleMultiplier = 1f)
        {
            if (cellCount == 0) return;

            var internalMultiplier = 1.5f;
            var buttonWidth = cellWidth * internalMultiplier * scaleMultiplier;
            var buttonHeight = cellHeight * internalMultiplier * scaleMultiplier;
            var aspectRatio = cellHeight / cellWidth;

            var verticalMargin = 5 * internalMultiplier;
            var horizontalMargin = 5 * internalMultiplier;
            float scrollBarAllowance = 6;
            var buttonWidthWithMargin = buttonWidth + horizontalMargin;
            var resultsPerLine = Mathf.Floor((rect.width - horizontalMargin) / buttonWidthWithMargin);
            if (resultsPerLine == 0)
            {
                resultsPerLine = 1;
                var scalingFix = scaleMultiplier;
                if (buttonWidth > rect.width)
                {
                    scalingFix = (rect.width / cellWidth) / internalMultiplier;
                    buttonWidth = rect.width;
                    buttonHeight = buttonWidth * aspectRatio;
                    buttonWidthWithMargin = buttonWidth + horizontalMargin;
                }
                scaleMultiplier = scalingFix;
            }
            var rows = (int)Math.Ceiling(cellCount / resultsPerLine);
            var actualBlockWidth = (resultsPerLine * buttonWidthWithMargin) + horizontalMargin;
            var outerRemainder = rect.width - actualBlockWidth;
            var remainderMargin = outerRemainder / 2;

            var gridArea = new Rect(rect.x, rect.y, rect.width + scrollBarAllowance, buttonHeight * rows + 
                verticalMargin * rows);
            var view = rect;
            newScrollPosition = GUI.BeginScrollView(view, newScrollPosition, gridArea, false, false, GUIStyle.none, 
                GUI.skin.verticalScrollbar);

            if (copiedToKeyboard)
            {
                if (!copiedRect.Contains(Event.current.mousePosition))
                {
                    copiedToKeyboard = false;
                }
            }
            var scrollViewRect = new Rect(new Vector2(view.x, view.y + newScrollPosition.y), view.size);


            // Iterate through rows and draw
            for (var yPos = 0; yPos < rows; yPos++)
            {
                var rowCoord = view.yMin + yPos * buttonHeight + verticalMargin * yPos;

                if (rowCoord > scrollViewRect.yMax) continue;
                if (rowCoord + buttonHeight < scrollViewRect.yMin) continue;

                for (var xPos = 0; xPos < resultsPerLine; xPos++)
                {
                    var columnCoord = view.xMin + xPos * buttonWidthWithMargin + horizontalMargin + 
                                      (remainderMargin - scrollBarAllowance);
                    var index = yPos * (int)resultsPerLine + xPos;

                    if (results.Count > index)
                    {
                        drawCellFunction(results, columnCoord, rowCoord, buttonWidth, buttonHeight, 
                            yPos * Mathf.FloorToInt(resultsPerLine) + xPos, scaleMultiplier);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            GUI.EndScrollView();
        }

        protected void DrawDetails()
        {
            Event e = Event.current;

            #region Back Button
            var backButtonStyle = new GUIStyle(IconStyle)
            {
                font = GetPoppinsFont(PoppinsStyle.SemiBold),
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                wordWrap = true,
                normal = new GUIStyleState
                {
                    textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black
                }
            };

            var backText = new GUIContent("Back", StateBackIcon.activeTexture);
            var backTextSize = backButtonStyle.CalcSize(backText);

            var backRect = GUILayoutUtility.GetRect(position.width, backTextSize.y * 1.5f);

            var margin = (backRect.height - backTextSize.y) / 2;

            var backIconRect = new Rect(backRect.x + margin, backRect.y + margin, backTextSize.x, backTextSize.y);
            if (backIconRect.Contains(e.mousePosition))
            {
                backText.image = StateBackIcon.hoverTexture;
                backButtonStyle.normal = SetStyleState(EditorGUIUtility.isProSkin ? HexToColour("606162") : HexToColour("EDEEEC"));
            }

            if (GUI.Button(backIconRect, backText, backButtonStyle))
            {
                selectedResult = null;
                return;
            }
            #endregion Back Button

            #region Thumbnail
            var thumbnailRect = GUILayoutUtility.GetRect(position.width, Mathf.Min(300f, position.height * 0.4f));
            GUI.DrawTexture(thumbnailRect, BaseDetailsThumbnailBackdrops[Mathf.Abs(selectedResult.GetHashCode()) % BaseDetailsThumbnailBackdrops.Length], ScaleMode.StretchToFill);
            GUI.DrawTexture(thumbnailRect, selectedResult.Thumbnail, ScaleMode.ScaleToFit);
            #endregion Thumbnail

            GUILayout.Space(10f);

            #region Intro
            var introRect = GUILayoutUtility.GetRect(position.width, 40f);

            var userIcon = selectedResult.data.author switch
            {
                "Anything World" => UserProfile_AnythingWorld,
                "Poly by Google" => UserProfile_GooglePoly,
                "Quaternius" => UserProfile_Quaternius,
                _ => BaseUserDefaultProfile
            };

            var userIconSize = introRect.height;
            var padding = introRect.height / 4;
            var userIconRect = new Rect(introRect.x + padding, introRect.y, userIconSize, userIconSize);
            GUI.DrawTexture(userIconRect, userIcon);

            var buttonFontSize = 12;
            var detailButtonPadding = 20f;
            var detailButtonStyle = new GUIStyle(BodyLabelStyle) { font = GetPoppinsFont(PoppinsStyle.Medium), fontSize = buttonFontSize, normal = SetStyleState(Color.white), hover = SetStyleState(Color.white), wordWrap = false, alignment = TextAnchor.MiddleCenter };

            var collectionContent = new GUIContent("Add to collection", BaseCollectionIcon);
            EditorGUIUtility.SetIconSize(Vector2.one * buttonFontSize);
            var collectionSize = detailButtonStyle.CalcSize(collectionContent);
            collectionSize.x += detailButtonPadding;
            collectionSize.y += detailButtonPadding;
            var collectionRect = new Rect(introRect.xMax - padding - collectionSize.x, introRect.y, collectionSize.x, collectionSize.y);

            if (!selectedResult.IsProcessedResult)
            {
                if (DrawRoundedButton(collectionRect, collectionContent, detailButtonStyle))
                {
                    CollectionProcessor.GetCollectionNamesAsync(CollectionReceived, EditorNetworkErrorHandler.HandleError, this).Forget();
                    void CollectionReceived(string[] results)
                    {
                        existingCollections = results;
                        AnythingSubwindow.OpenWindow("Add to Collection", new Vector2(Mathf.Max(position.width - 80, 450), 200), DrawCollectionWindow, position, selectedResult);
                    }
                }
            }

            var voteContent = new GUIContent($"{TruncateNumber(selectedResult.data.voteScore)} Like{(selectedResult.data.voteScore == 1 ? "" : "s")}", selectedResult.data.userVote == "upvote" ? StateHeartIcon.activeTexture : StateHeartIcon.inactiveTexture);
            EditorGUIUtility.SetIconSize(Vector2.one * buttonFontSize);
            var voteSize = detailButtonStyle.CalcSize(voteContent);
            voteSize.x += detailButtonPadding;
            voteSize.y += detailButtonPadding;
            var voteRect = new Rect(collectionRect.x - padding - voteSize.x, introRect.y, voteSize.x, voteSize.y);

            if (!selectedResult.IsProcessedResult)
            {
                if (DrawRoundedButton(voteRect, voteContent, detailButtonStyle))
                {
                    UserVoteProcessor.FlipUserVote(selectedResult, EditorNetworkErrorHandler.HandleError, this);
                }
            }
            var introLabelHeight = (userIconRect.height / 2) - 2.5f;
            var modelName = new GUIContent($"{selectedResult.DisplayName} ({selectedResult.name})");
            var modelNameStyle = new GUIStyle(ModelNameStyle) { fontSize = 14, clipping = TextClipping.Clip };
            var modelNameXPos = userIconRect.xMax + padding / 2;
            var modelNameLabelWidth = modelNameStyle.CalcSize(modelName).x;
            var modelNameLabelRect = new Rect(modelNameXPos, userIconRect.center.y - introLabelHeight, Mathf.Min(modelNameLabelWidth, voteRect.x - padding - modelNameXPos), introLabelHeight);
            GUI.Label(modelNameLabelRect, modelName, modelNameStyle);

            var authorName = new GUIContent(selectedResult.data.author);
            var authorStyle = new GUIStyle(AuthorNameStyle) { fontSize = 14, clipping = TextClipping.Clip };
            var authorNameXPos = userIconRect.xMax + padding / 2;
            var authorNameLabelWidth = authorStyle.CalcSize(authorName).x;
            var authorNameLabelRect = new Rect(authorNameXPos, userIconRect.center.y, Mathf.Min(authorNameLabelWidth, voteRect.x - padding - modelNameXPos), introLabelHeight);
            GUI.Label(authorNameLabelRect, authorName, authorStyle);
            #endregion Intro

            #region Details
            GUILayout.Space(20f);
            var detailsRect = GUILayoutUtility.GetRect(position.width, 30f);
            var iconSize = 20f;
            padding = detailsRect.height / 4;
            float nextXPosStart = detailsRect.x;

            var detailStyle = new GUIStyle(VoteStyle) { normal = SetStyleState(HexToColour("979797")), hover = SetStyleState(HexToColour("979797")), fontSize = 12, font = GetPoppinsFont(PoppinsStyle.Regular) };

            if (selectedResult.data.type != null)
            {
                var typeContent = new GUIContent($"{selectedResult.data.type.DeepClean().CapitaliseAll()}");
                var typeLabelSize = detailStyle.CalcSize(typeContent);

                var typeRect = new Rect(nextXPosStart + padding, detailsRect.center.y - (iconSize / 2), iconSize, iconSize);
                GUI.DrawTexture(typeRect, BaseTypeIcon, ScaleMode.ScaleToFit);

                var typeLabelRect = new Rect(typeRect.xMax + padding / 2, detailsRect.y + (detailsRect.height - typeLabelSize.y) / 2, typeLabelSize.x, typeLabelSize.y);
                GUI.Label(typeLabelRect, typeContent, detailStyle);
                nextXPosStart = typeLabelRect.xMax + padding;
            }

            if (selectedResult.data.poly_count.ContainsKey("original") || !string.IsNullOrEmpty(selectedResult.data.detail))
            {
                var polyCountTooltip = (selectedResult.data.poly_count.ContainsKey("original") ? $"{selectedResult.data.poly_count["original"]}" : "N/A") + " Polygons\n"
                                     + (selectedResult.data.vert_count.ContainsKey("original") ? $"{selectedResult.data.vert_count["original"]}" : "N/A") + " Vertices";

                var polyCountContent = new GUIContent($"{(selectedResult.data.detail != null ? $"{selectedResult.data.detail.DeepClean().CapitaliseAll()} Poly Count" : $"{selectedResult.data.poly_count["original"]} Polygons")}", polyCountTooltip);
                var polyCountLabelSize = detailStyle.CalcSize(polyCountContent);

                var polyCountRect = new Rect(nextXPosStart + padding, detailsRect.center.y - (iconSize / 2), iconSize, iconSize);
                GUI.DrawTexture(polyCountRect, BasePolyCountIcon, ScaleMode.ScaleToFit);

                var polyCountLabelRect = new Rect(polyCountRect.xMax + padding / 2, detailsRect.y + (detailsRect.height - polyCountLabelSize.y) / 2, polyCountLabelSize.x, polyCountLabelSize.y);
                GUI.Label(polyCountLabelRect, polyCountContent, detailStyle);
                nextXPosStart = polyCountLabelRect.xMax + padding;
            }

            var licenseContent = new GUIContent($"CC BY 4.0", "You must give appropriate credit, provide a link to the license, and indicate if changes were made. You may do so in any reasonable manner, but not in any way that suggests the licensor endorses you or your use. [https://creativecommons.org/licenses/by/4.0/]");
            var licenseLabelSize = detailStyle.CalcSize(licenseContent);

            var licenseRect = new Rect(nextXPosStart + padding, detailsRect.center.y - (iconSize / 2), iconSize, iconSize);
            GUI.DrawTexture(licenseRect, BaseLicenseIcon, ScaleMode.ScaleToFit);

            var licenseLabelRect = new Rect(licenseRect.xMax + padding / 2, detailsRect.y + (detailsRect.height - licenseLabelSize.y) / 2, licenseLabelSize.x, licenseLabelSize.y);
            GUI.Label(licenseLabelRect, licenseContent, detailStyle);

            var labelHeight = 20f;
            var labelPadding = 6f;
            if (selectedResult.data.themeCategories != null && selectedResult.data.themeCategories.Any())
            {
                var categoryLabelStartingPositions = CalcAutoSizeLabelPositions(selectedResult.data.themeCategories, out var totalHeight, labelPadding, iconSize, labelHeight, 12, PoppinsStyle.Regular);

                GUILayout.Space(10);
                var categoryRect = GUILayoutUtility.GetRect(position.width, totalHeight);

                var categoryIconRect = new Rect(categoryRect.x + padding, categoryRect.y, iconSize, iconSize);
                GUI.DrawTexture(categoryIconRect, BaseCategoryIcon, ScaleMode.ScaleToFit);

                var categoryLabelsRect = new Rect(categoryIconRect.xMax + padding, categoryRect.y, categoryRect.width - categoryIconRect.width - padding, categoryRect.height);

                for (int i = 0; i < categoryLabelStartingPositions.Length; i++)
                {
                    DrawAutoSizeRoundedLabel(categoryLabelsRect.position + categoryLabelStartingPositions[i], new GUIContent(selectedResult.data.themeCategories[i].CapitaliseAll()), labelHeight, 12, PoppinsStyle.Regular);
                }
            }

            if (selectedResult.data.tags != null && selectedResult.data.tags.Any())
            {
                var tagLabelStartingPositions = CalcAutoSizeLabelPositions(selectedResult.data.tags, out var totalLabelHeight, labelPadding, iconSize, labelHeight, 12, PoppinsStyle.Regular);

                GUILayout.Space(10);
                var tagRect = GUILayoutUtility.GetRect(position.width, totalLabelHeight);

                var tagIconRect = new Rect(tagRect.x + padding, tagRect.y, iconSize, iconSize);
                GUI.DrawTexture(tagIconRect, BaseTagIcon, ScaleMode.ScaleToFit);

                var tagLabelsRect = new Rect(tagIconRect.xMax + padding, tagRect.y, tagRect.width - tagIconRect.width - padding, tagRect.height);

                for (int i = 0; i < tagLabelStartingPositions.Length; i++)
                {
                    DrawAutoSizeRoundedLabel(tagLabelsRect.position + tagLabelStartingPositions[i], new GUIContent(selectedResult.data.tags[i].CapitaliseAll()), labelHeight, 12, PoppinsStyle.Regular);
                }

            }

            if (selectedResult.data.habitats != null && selectedResult.data.habitats.Any())
            {
                var habitatLabelStartingPositions = CalcAutoSizeLabelPositions(selectedResult.data.habitats, out var totalHeight, labelPadding, iconSize, labelHeight, 12, PoppinsStyle.Regular);

                GUILayout.Space(10);
                var habitatRect = GUILayoutUtility.GetRect(position.width, totalHeight);

                var habitatIconRect = new Rect(habitatRect.x + padding, habitatRect.y, iconSize, iconSize);
                GUI.DrawTexture(habitatIconRect, BaseEnvironmentIcon, ScaleMode.ScaleToFit);

                var habitatLabelsRect = new Rect(habitatIconRect.xMax + padding, habitatRect.y, habitatRect.width - habitatIconRect.width - padding, habitatRect.height);

                for (int i = 0; i < habitatLabelStartingPositions.Length; i++)
                {
                    DrawAutoSizeRoundedLabel(habitatLabelsRect.position + habitatLabelStartingPositions[i], new GUIContent(selectedResult.data.habitats[i].CapitaliseAll()), labelHeight, 12, PoppinsStyle.Regular);
                }
            }
            #endregion Details

            GUILayout.FlexibleSpace();
            var creationRectBaseSpacer = 12f;
            var creationRect = GUILayoutUtility.GetRect(position.width, 40);
            var creationRectMargin = 12f;
            var creationRectPadding = 6f;
            var creationRectWidth = creationRect.width - (creationRectPadding) - (creationRectMargin * 2);

            creationRect.y -= creationRectBaseSpacer;

            var reportRect = new Rect(creationRect.x + creationRectMargin, creationRect.y, creationRectWidth * 0.2f, creationRect.height);
            var addToSceneRect = new Rect(reportRect.xMax + creationRectPadding, creationRect.y, creationRectWidth * 0.4f, creationRect.height);
            //in processed models the menu must show diferent options
            if (!selectedResult.IsProcessedResult)
            {
                if (DrawRoundedButton(reportRect, new GUIContent("Report", TintedReportIcon), 12, PoppinsStyle.SemiBold))
                {
                    AnythingSubwindow.OpenWindow($"Report {selectedResult.DisplayName}", new Vector2(Mathf.Max(position.width - 80, 450), 160), DrawReportWindow, position, selectedResult);
                }



                if (DrawRoundedButton(addToSceneRect, new GUIContent("Add Model to Scene"), 12, PoppinsStyle.SemiBold))
                {
                    MakeResult(selectedResult);
                }
            }
            else
            {
                Rect rect = new Rect(creationRect.x + creationRectMargin, addToSceneRect.y, creationRectWidth, addToSceneRect.height);

                if (DrawRoundedButton(rect, new GUIContent("Add Model to Scene"), 12, PoppinsStyle.SemiBold))
                {
                    MakeResult(selectedResult);
                }
            }

            var addAndSerializeRect = new Rect(addToSceneRect.xMax + creationRectPadding, creationRect.y, creationRectWidth * 0.4f, creationRect.height);

            if (!selectedResult.IsProcessedResult)
            {
                if (DrawRoundedButton(addAndSerializeRect, new GUIContent("Add Model and Save Assets"), 12, PoppinsStyle.SemiBold))
                {
                    MakeResult(selectedResult, true);
                }
            }
        }
        public string[] existingCollections;
        public int existingCollectionIndex = 0;
        public string collectionSearchTerm;

        public void SetExistingIndex(int i)
        {
            existingCollectionIndex = i;
        }

        protected void DrawCollectionWindow(AnythingEditor window, SearchResult result)
        {
            var margin = 20f;
            var padding = 10f;
            var contentWidth = window.position.width - (margin * 2);
            var actionWidth = (contentWidth - padding) * 0.75f;
            var buttonWidth = (contentWidth - padding) * 0.25f;

            var labelStyle = new GUIStyle() { font = GetPoppinsFont(PoppinsStyle.SemiBold), fontSize = 12, normal = SetStyleState(Color.white), hover = SetStyleState(Color.white) };
            var existingLabelContent = new GUIContent($"Add \"{result.DisplayName}\" to a collection");
            var existingLabelSize = new Vector2(contentWidth, labelStyle.CalcHeight(existingLabelContent, contentWidth));
            var existingLabelRect = new Rect(margin, margin, existingLabelSize.x, existingLabelSize.y);

            GUI.Label(existingLabelRect, existingLabelContent, labelStyle);

            List<DropdownOption> newCollections = new List<DropdownOption>();

            for (int i = 0; i < existingCollections.Length; i++)
            {
                string collectionName = existingCollections[i];
                int index = i;
                var dropdown = new DropdownOption()
                {
                    dataEndpoint = index,
                    label = collectionName,
                    function = () => existingCollectionIndex = index
                };
                newCollections.Add(dropdown);
            }
            DropdownOption[] createdCollections = newCollections.ToArray();

            var existingCollectionDropdownRect = new Rect(margin, existingLabelRect.yMax + padding, actionWidth, 40);
            DrawDropdown(existingCollectionDropdownRect, createdCollections, existingCollectionIndex, "", 10, existingCollections.Any());

            var existingCollectionButtonRect = new Rect(existingCollectionDropdownRect.xMax + padding, existingLabelRect.yMax + padding, buttonWidth, 40);
            if (DrawRoundedButton(existingCollectionButtonRect, new GUIContent("Add", BaseCollectionIcon)) && existingCollections.Any())
            {
                CollectionProcessor.AddToCollectionAsync(MyWorldEditor.Instance.RefreshCollectionResults, result, existingCollections[existingCollectionIndex], EditorNetworkErrorHandler.HandleError, this).Forget();
                window.Close();
            }

            var newLabelContent = new GUIContent($"Add \"{result.DisplayName}\" to a new collection");
            var newLabelSize = new Vector2(contentWidth, labelStyle.CalcHeight(newLabelContent, contentWidth));
            var newLabelRect = new Rect(margin, existingCollectionDropdownRect.yMax + margin, newLabelSize.x, newLabelSize.y);

            GUI.Label(newLabelRect, newLabelContent, labelStyle);

            bool changed = false;
            var newCollectionInputFieldRect = new Rect(margin, newLabelRect.yMax + padding, actionWidth, 40);
            collectionSearchTerm = DrawRoundedInputField(newCollectionInputFieldRect, collectionSearchTerm, ref changed);

            var newCollectionButtonRect = new Rect(newCollectionInputFieldRect.xMax + padding, newLabelRect.yMax + padding, buttonWidth, 40);
            if (DrawRoundedButton(newCollectionButtonRect, new GUIContent("Create", BaseCollectionIcon)) && !string.IsNullOrEmpty(collectionSearchTerm))
            {
                CollectionProcessor.AddToCollectionAsync(MyWorldEditor.Instance.RefreshCollectionResults, result, collectionSearchTerm, EditorNetworkErrorHandler.HandleError, this).Forget();

                collectionSearchTerm = "";
                window.Close();
            }
        }

        protected void SetupResetEditorWindow(ResetMode resetMode)
        {
            string inlineResetText = resetMode switch
            {
                ResetMode.Scene => "the scene",
                ResetMode.Creator => "the creator",
                ResetMode.All => "everything",
                _ => ""
            };
            DisplayAWDialog($"Reset {resetMode}?", $"Are you sure you want to reset {inlineResetText}?", $"Reset {resetMode}", "Cancel", () => ResetAnythingWorld(resetMode));
        }

        bool reportSent;
        ReportProcessor.ReportReason reportReason = ReportProcessor.ReportReason.COPYRIGHT;

        private void DrawReportWindow(AnythingEditor window, SearchResult result)
        {
            var margin = 20f;
            var padding = 10f;
            var contentWidth = window.position.width - (margin * 2);
        
            var labelStyle = new GUIStyle() { font = GetPoppinsFont(PoppinsStyle.SemiBold), fontSize = 16, normal = SetStyleState(Color.white), hover = SetStyleState(Color.white) };
            var labelContent = new GUIContent(reportSent ? "Thank you for your report!" : "Please let us know why you’re reporting this model.");
            var labelSize = new Vector2(contentWidth, labelStyle.CalcHeight(labelContent, contentWidth));
            var labelRect = new Rect(margin, margin, labelSize.x, labelSize.y);
        
            GUI.Label(labelRect, labelContent, labelStyle);
        
            if (!reportSent)
            {
                DropdownOption[] reportOptions = new DropdownOption[]
                {
                    new DropdownOption
                    {
                        dataEndpoint = ReportProcessor.ReportReason.COPYRIGHT,
                        label = "Copyright",
                        function = () => SetReportStatus(ReportProcessor.ReportReason.COPYRIGHT)
                    },
                    new DropdownOption
                    {
                        dataEndpoint = ReportProcessor.ReportReason.EMPTY,
                        label = "Empty Model",
                        function = () => SetReportStatus(ReportProcessor.ReportReason.EMPTY)
                    },
                    new DropdownOption
                    {
                        dataEndpoint = ReportProcessor.ReportReason.INAPPROPRIATE,
                        label = "Inappropriate",
                        function = () => SetReportStatus(ReportProcessor.ReportReason.INAPPROPRIATE)
                    },
                    new DropdownOption
                    {
                        dataEndpoint = ReportProcessor.ReportReason.QUALITY,
                        label = "Poor Quality",
                        function = () => SetReportStatus(ReportProcessor.ReportReason.QUALITY)
                    },
                    new DropdownOption
                    {
                        dataEndpoint = ReportProcessor.ReportReason.OTHER,
                        label = "Other",
                        function = () => SetReportStatus(ReportProcessor.ReportReason.OTHER)
                    }
                };
        
                var reportObjectRect = new Rect(margin, labelRect.yMax + padding, contentWidth, 40);
                DrawDropdown(reportObjectRect, reportOptions, reportReason);
        
                var buttonWidth = window.position.width * 0.3f;
                var reportRect = new Rect(window.position.width - margin - buttonWidth, reportObjectRect.yMax + padding, buttonWidth, 40);
                if (DrawRoundedButton(reportRect, new GUIContent("Send Report")))
                {
                    ReportProcessor.SendReport(DrawPostReportSent, result, reportReason, EditorNetworkErrorHandler.HandleError);
                }
            }
        
            void DrawPostReportSent()
            {
                UniTask.Void(async () =>
                {
                    reportSent = true;
                    await UniTask.Delay(TimeSpan.FromSeconds(3));
                    window.Close();
                    reportSent = false;
                });
            }
        
            void SetReportStatus(ReportProcessor.ReportReason reason)
            {
                reportReason = reason;
            }
        }
        #endregion Editor Drawing

        #region Helper Functions
        protected enum ResetMode
        {
            Scene, Creator, All
        }

        protected virtual void ResetAnythingWorld(ResetMode resetMode)
        {
            if (resetMode != ResetMode.Scene)
            {
                resultThumbnailMultiplier = 1f;
                searchModeFailReason = "";
                searchMode = SearchMode.IDLE;

                currentAnimationFilter = AnimatedDropdownOption.BOTH;
                currentCategory = CategoryDropdownOption.ALL;
                currentSortingMethod = SortingDropdownOption.MostRelevant;

                CloseWindowIfOpen<TransformSettingsEditor>();

                TransformSettings.ResetSettings();

                CachedModelsRepository.Clear();

                AssignDefaultBehavioursFromScriptable();
            }

            if (resetMode != ResetMode.Creator)
            {
                CachedModelsRepository.Clear();
                ModelDataInspector[] createdModels = FindObjectsByType<ModelDataInspector>(FindObjectsSortMode.None);
                for (int i = createdModels.Length - 1; i >= 0; i--)
                {
                    Utilities.Destroy.GameObject(createdModels[i].gameObject);
                }
                SimpleGrid.Reset();
            }
        }

        public void UpdateSearchResults(SearchResult[] results, string onEmpty)
        {
            searchResults = new List<SearchResult>();
            searchMode = SearchMode.SUCCESS;

            if (results == null || results.Length == 0)
            {
                searchMode = SearchMode.FAILURE;
                searchModeFailReason = onEmpty;
                return;
            }

            if (results.Length > 0)
            {
                searchResults = results.ToList();
                FilterSearchResult(searchResults);
            }
        }

        public void UpdateSearchResults(ref List<SearchResult> unfiltered, ref List<SearchResult> filtered, string onEmpty)
        {
            unfiltered = new List<SearchResult>();

            if (unfiltered == null || unfiltered.Count == 0)
            {
                return;
            }

            if (unfiltered.Count > 0)
            {
                searchResults = unfiltered.ToList();
                filtered = FilterAndReturnResults(searchResults);
            }
        }
        public List<SearchResult> FilterAndReturnResults(List<SearchResult> results)
        {
            var filtered = FilterByAnimation(results);
            filtered = FilterByCategory(filtered);
            filtered = SortResults(filtered);
            filteredResults = filtered;
            return filteredResults;
        }
        public void FilterSearchResult(List<SearchResult> results)
        {
            var filtered = FilterByAnimation(results);
            filtered = FilterByCategory(filtered);
            filtered = SortResults(filtered);
            filteredResults = filtered;
            if (filteredResults == null)
            {
                searchMode = SearchMode.FAILURE;
                searchModeFailReason = "We couldn't find any models matching those filters.";
            }
            Repaint();
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Filters a list of search results by category.
        /// </summary>
        /// <param name="results">The list of search results to be filtered.</param>
        /// <returns>A filtered list of search results based on the current category.</returns>
        private List<SearchResult> FilterByCategory(List<SearchResult> results)
        {
            var categoryFilter = new List<SearchResult>();

            if (currentCategory == CategoryDropdownOption.ALL) return results;

            string categoryWord = currentCategory.ToString().ToLower();
            categoryFilter = (from result in results where result.data.themeCategories.Contains(categoryWord) select result).ToList();

            return categoryFilter;
        }

        private List<SearchResult> FilterByAnimation(List<SearchResult> results)
        {
            List<SearchResult> animationFilter = new List<SearchResult>();
            switch (currentAnimationFilter)
            {
                case AnimatedDropdownOption.BOTH:
                    animationFilter = results;
                    break;
                case AnimatedDropdownOption.ANIMATED:
                    animationFilter = (from result in results where result.isAnimated select result).ToList();
                    break;
                case AnimatedDropdownOption.STILL:
                    animationFilter = (from result in results where !result.isAnimated select result).ToList();
                    break;
            }
            return animationFilter;
        }

        private List<SearchResult> SortResults(List<SearchResult> results)
        {
            List<SearchResult> sortedResults = new List<SearchResult>();
            switch (currentSortingMethod)
            {
                case SortingDropdownOption.MostRelevant:
                    sortedResults = results;
                    break;
                case SortingDropdownOption.MostPopular:
                    sortedResults = (from result in results orderby result.data.popularity select result).ToList();
                    sortedResults.Reverse();
                    break;
                case SortingDropdownOption.MostLiked:
                    sortedResults = (from result in results orderby result.data.voteScore select result).ToList();
                    sortedResults.Reverse();
                    break;
                case SortingDropdownOption.MyList:
                    sortedResults = (from result in results where result.data.userVote == "upvote" select result).ToList();
                    break;
                case SortingDropdownOption.AtoZ:
                    sortedResults = (from result in results orderby result.data.name select result).ToList();
                    break;
                case SortingDropdownOption.ZtoA:
                    sortedResults = (from result in results orderby result.data.name select result).ToList();
                    sortedResults.Reverse();
                    break;
            }

            return sortedResults;
        }

        protected string TruncateNumber(int number)
        {
            switch (number)
            {
                case var _ when number >= 100000000:
                    return (number / 1000000).ToString("#,0M");
                case var _ when number >= 10000000:
                    return (number / 1000000).ToString("0.#") + "M";
                case var _ when number >= 100000:
                    return (number / 1000).ToString("#,0K");
                case var _ when number >= 10000:
                    return (number / 1000).ToString("0.#") + "K";
                default:
                    return number.ToString("#,0");
            };
        }
        #endregion Helper Functions
    }

    public class AnythingSubwindow : AnythingEditor
    {
        protected static bool windowOpen = false;
        protected static Rect windowPosition;
        protected static Rect callingWindowScreenPosition;

        protected static bool invokeWithParameter;
        protected static bool resetWindowPosition = true;

        protected static string windowTitle;
        protected static Vector2 windowSize;

        protected static Action<AnythingEditor> windowAction;
        protected static Action<AnythingEditor, SearchResult> windowActionSR;
        protected static SearchResult searchResult;

        public static void OpenWindow(string title, Vector2 size, Action<AnythingEditor, SearchResult> guiAction, Rect callingWindow, SearchResult result)
        {
            callingWindowScreenPosition = GUIUtility.GUIToScreenRect(callingWindow);
            windowTitle = title;
            windowSize = size;
            windowActionSR = guiAction;
            searchResult = result;

            invokeWithParameter = true;
            ShowWindow();
        }

        public static void OpenWindow(string title, Vector2 size, Action<AnythingEditor> guiAction, Rect callingWindow)
        {
            callingWindowScreenPosition = GUIUtility.GUIToScreenRect(callingWindow);
            windowTitle = title;
            windowSize = size;
            windowAction = guiAction;

            invokeWithParameter = false;
            ShowWindow();
        }

        protected static void ShowWindow()
        {
            var window = GetWindow<AnythingSubwindow>(true);
            window.titleContent = new GUIContent(windowTitle);
            window.minSize = window.maxSize = windowSize;

            if (resetWindowPosition)
            {
                resetWindowPosition = false;
                windowPosition = GUIUtility.ScreenToGUIRect(new Rect(callingWindowScreenPosition.x + ((callingWindowScreenPosition.width - window.minSize.x) / 2), callingWindowScreenPosition.y + ((callingWindowScreenPosition.height - window.minSize.y) / 2), 0, 0));
            }
            else
            {
                windowPosition = window.position;
            }
            //If failed to find width give default subvalue;
            if (windowPosition.width == 0) windowPosition.width = 450;
            if (windowPosition.height == 0) windowPosition.height = 260;
            window.position = windowPosition;
            windowOpen = true;
        }

        protected new void OnGUI()
        {
            base.OnGUI();
            if (!windowOpen) Close();
            if (invokeWithParameter) windowActionSR?.Invoke(this, searchResult);
            else windowAction?.Invoke(this);

        }

        protected void OnDestroy()
        {
            resetWindowPosition = true;
            // When the window is destroyed, remove the delegate
            // so that it will no longer do any drawing.
        }
    }
}
