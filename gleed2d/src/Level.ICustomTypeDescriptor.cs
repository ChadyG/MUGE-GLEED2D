using System;
using System.ComponentModel;
using CustomUITypeEditors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GLEED2D
{
    
 
    public partial class Level : ICustomTypeDescriptor
    {

        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        string ICustomTypeDescriptor.GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorCollection pdc = new PropertyDescriptorCollection(new PropertyDescriptor[0]);
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(this))
            {
                pdc.Add(pd);
            }
            foreach (String key in CustomProperties.Keys)
            {
                pdc.Add(new DictionaryPropertyDescriptor(CustomProperties, key, attributes));
            }
            return pdc;
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return TypeDescriptor.GetProperties(this, true);
        }

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

    }



    public class DictionaryPropertyDescriptor : PropertyDescriptor
    {
        String key;
        public SerializableDictionary sdict;


        public DictionaryPropertyDescriptor(SerializableDictionary dict, String key, Attribute[] attrs)
            : base(key, attrs)
        {
            this.key = key;
            this.sdict = dict;
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override Type ComponentType
        {
            get { return null; }
        }

        public override object GetValue(object component)
        {
            return sdict[key].value;
        }

        public override string Description
        {
            get { return sdict[key].description; }
        }

        public override string Category
        {
            get { return "Custom Properties"; }
        }

        public override string DisplayName
        {
            get { return key; }
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override void ResetValue(object component)
        {
            //Have to implement
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        public override void SetValue(object component, object value)
        {
            sdict[key].value = value;
        }

        public override Type PropertyType
        {
            get { return sdict[key].type; }
        }

        public override object GetEditor(Type editorBaseType)
        {
            if (sdict[key].type == typeof(Vector2)) return new Vector2UITypeEditor();
            if (sdict[key].type == typeof(Color)) return  new XNAColorUITypeEditor();
            if (sdict[key].type == typeof(Item)) return new ItemUITypeEditor();
            
            return base.GetEditor(editorBaseType);
        }

    }




}
