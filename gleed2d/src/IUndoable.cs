using System;
namespace GLEED2D
{
    public interface IUndoable
    {

        IUndoable cloneforundo();

        void makelike(IUndoable other);
        
    }
}
