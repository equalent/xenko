// Copyright (c) 2011-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;
using System.ComponentModel;
using SiliconStudio.Assets;
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Xenko.UI;

namespace SiliconStudio.Xenko.Assets.UI
{
    /// <summary>
    /// Associate an <see cref="UIElement"/> with design-time data.
    /// </summary>
    [DataContract("UIElementDesign")]
    public sealed class UIElementDesign : IAssetPartDesign<UIElement>, IEquatable<UIElementDesign>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UIElementDesign"/>.
        /// </summary>
        /// <remarks>
        /// This constructor is used only for serialization.
        /// </remarks>
        public UIElementDesign()
            // ReSharper disable once AssignNullToNotNullAttribute
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="UIElementDesign"/>.
        /// </summary>
        /// <param name="uiElement">The UI Element</param>
        public UIElementDesign([NotNull] UIElement uiElement)
        {
            UIElement = uiElement;
        }

        /// <summary>
        /// The UI element.
        /// </summary>
        /// <remarks>
        /// The setter should only be used during serialization.
        /// </remarks>
        [DataMember(10)]
        [NotNull]
        public UIElement UIElement { get; set; }

        /// <inheritdoc/>
        [DataMember(20)]
        [DefaultValue(null)]
        public BasePart Base { get; set; }

        /// <inheritdoc/>
        IIdentifiable IAssetPartDesign.Part => UIElement;

        /// <inheritdoc/>
        UIElement IAssetPartDesign<UIElement>.Part => UIElement;

        /// <inheritdoc />
        public bool Equals(UIElementDesign other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return UIElement.Equals(other.UIElement) && Equals(Base, other.Base);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as UIElementDesign);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode - this property is not supposed to be changed, except in initializers
            return UIElement.GetHashCode();
        }

        public static bool operator ==(UIElementDesign left, UIElementDesign right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(UIElementDesign left, UIElementDesign right)
        {
            return !Equals(left, right);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"UIElementDesign [{UIElement.GetType().Name}, {UIElement.Name}]";
        }
    }
}
