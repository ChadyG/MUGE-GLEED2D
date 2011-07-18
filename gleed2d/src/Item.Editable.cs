using System.ComponentModel;
using System.Drawing.Design;
using System.Xml.Serialization;
using CustomUITypeEditors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Forms = System.Windows.Forms;
using System.Windows.Forms;

namespace GLEED2D
{
    public class ItemTypeConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType)
        {
            if (destinationType == typeof(Item)) return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType)
        {
            if (destinationType == typeof(string) && value is Item)
            {
                Item result = (Item)value;
                return result.Name;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

    }

    [TypeConverter(typeof(ItemTypeConverter))]
    public abstract partial class Item
    {
        [XmlIgnore()]
        public Layer layer;

        protected bool hovering;

        [XmlIgnore()]
        [DisplayName("Position"), Category(" Transform")]
        [Description("The item's position in world space.")]
        public Vector2 pPosition
        {
            get
            {
                return Position;
            }
            set
            {
                Position = value;
                OnTransformed();
            }
        }

        public virtual string getNamePrefix()
        {
            return "Item_";
        }

        public virtual void OnTransformed()
        {
        }

        public abstract Item clone();

        public virtual bool loadIntoEditor()
        {
            OnTransformed();
            return true;
        }

        public abstract void drawInEditor(SpriteBatch sb);
        
        public abstract void drawSelectionFrame(SpriteBatch sb, Matrix matrix, Color color);

        public virtual void onMouseOver(Vector2 mouseworldpos)
        {
            hovering = true;
        }

        public virtual void onMouseOut()
        {
            hovering = false;
            MainForm.Instance.pictureBox1.Cursor = Cursors.Default;
        }

        public virtual void onMouseButtonDown(Vector2 mouseworldpos)
        {
        }

        public virtual void onMouseButtonUp(Vector2 mouseworldpos)
        {
        }
        


        public virtual void setPosition(Vector2 pos)
        {
            pPosition = pos;
        }

        public virtual bool CanRotate()
        {
            return false;
        }
        public virtual float getRotation()
        {
            return 0;
        }
        public virtual void setRotation(float rotation)
        {
        }
        
        public virtual bool CanScale()
        {
            return false;
        }
        public virtual Vector2 getScale()
        {
            return Vector2.One;
        }
        public virtual void setScale(Vector2 scale)
        {
        }


        public abstract bool contains(Vector2 worldpos);



    }
}
