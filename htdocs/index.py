from System import *
from System.IO import *
import clr 
clr.AddReference("ImgurDotNet")
from ImgurDotNet import *

echo('<html><head><title>Hello World!</title></head><body><h1>Hello World</h1><br/>Text served by IronPython<3</body></html>');
#session = get_session()
imgur = Imgur()
album = imgur.GetAlbum("test")