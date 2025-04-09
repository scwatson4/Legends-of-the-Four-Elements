namespace AnythingWorld.Utilities
{
    /// <summary>
    /// Represents the type of animation pipeline used for a game asset.
    /// </summary>
    public enum AnimationPipeline
    {
        /// <summary>
        /// The animation pipeline type is not set.
        /// </summary>
        Unset,

        /// <summary>
        /// The animation pipeline uses static animations.
        /// </summary>
        Static,

        /// <summary>
        /// The animation pipeline uses rigged animations.
        /// </summary>
        Rigged,

        /// <summary>
        /// The animation pipeline uses animations for wheeled vehicles.
        /// </summary>
        WheeledVehicle,

        /// <summary>
        /// The animation pipeline uses animations for propeller-driven vehicles.
        /// </summary>
        PropellorVehicle,

        /// <summary>
        /// The animation pipeline uses shader-based animations.
        /// </summary>
        Shader
    }


    /// <summary>
    /// Represents the type of model loading pipeline used for a game asset.
    /// </summary>
    public enum ModelLoadingPipeline
    {
        /// <summary>
        /// The model loading pipeline type is not set.
        /// </summary>
        Unset,

        /// <summary>
        /// The model loading pipeline uses a rigged GLB loader.
        /// </summary>
        RiggedGLB,

        /// <summary>
        /// The model loading pipeline uses a GLTF loader.
        /// </summary>
        GLTF,

        /// <summary>
        /// The model loading pipeline uses an OBJ loader for static models.
        /// </summary>
        OBJ_Static,

        /// <summary>
        /// The model loading pipeline uses an OBJ loader for part-based models.
        /// </summary>
        OBJ_Part_Based
    }

    /// <summary>
    /// Represents the type of scaling to be applied to a game asset.
    /// </summary>
    public enum ScaleType
    {
        /// <summary>
        /// Scale the model to fit inside a box of the provided scale.
        /// </summary>
        SetRealWorld,

        /// <summary>
        /// Scale the model to size provided by the database, then scale the top-level transform by the provided input.
        /// </summary>
        ScaleRealWorld,

        /// <summary>
        /// Scale the model and rig to the input scale vector, but do not change the top-level transform (EXPERIMENTAL).
        /// </summary>
        Absolute
    }


    /// <summary>
    /// Represents the space in which the transform parameters are applied.
    /// </summary>
    public enum TransformSpace
    {
        /// <summary>
        /// The transform parameters are applied in local space.
        /// </summary>
        Local,

        /// <summary>
        /// The transform parameters are applied in world space.
        /// </summary>
        World
    }


    /// <summary>
    /// Represents the type of request to be made to the API.
    /// </summary>
    public enum RequestType
    {
        /// <summary>
        /// The request is a search request.
        /// </summary>
        Search,

        /// <summary>
        /// The request is a JSON request.
        /// </summary>
        Json,

        /// <summary>
        /// The request is a pre-processed JSON request.
        /// </summary>
        Processed,

        /// <summary>
        /// The request is a by ID request.
        /// </summary>
        Id
    }

    /// <summary>
    /// Represents the sorting options for a dropdown menu.
    /// </summary>
    public enum SortingDropdownOption
    {
        /// <summary>
        /// Sort by most relevant items.
        /// </summary>
        MostRelevant,

        /// <summary>
        /// Sort by most popular items.
        /// </summary>
        MostPopular,

        /// <summary>
        /// Sort by most liked items.
        /// </summary>
        MostLiked,

        /// <summary>
        /// Sort items alphabetically from A to Z.
        /// </summary>
        AtoZ,

        /// <summary>
        /// Sort items alphabetically from Z to A.
        /// </summary>
        ZtoA,

        /// <summary>
        /// Sort by items in user's list.
        /// </summary>
        MyList
    }

}