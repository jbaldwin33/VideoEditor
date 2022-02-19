using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoUtilities;

namespace VideoEditorUi.Services
{
    public interface IVideoEditorFactory
    {
        BaseClass GetVideoEditor();
    }

    public interface IVideoEditorService
    {
        BaseClass CreateVideoEditor();
        void SetupVideoEditor();
        void ExecuteVideoEditor();
    }

    public class VideoSplitterFactory : IVideoEditorFactory
    {
        public VideoSplitterFactory()
        {

        }

        public BaseClass GetVideoEditor()
        {
            throw new NotImplementedException();
        }
    }
}
