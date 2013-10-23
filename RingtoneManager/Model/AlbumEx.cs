using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Xna.Framework.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.IO;

namespace RingtoneManager
{
    public class AlbumEx :  BaseViewModel
    {
        private Album _base;
        public Album Base
        {
            get
            {
                return _base;
            }
            set
            {
                _base = value;
                OnChange("Base");
            }
        }

        private BitmapImage _thumbnail = null;
        private bool _thumbnailChecked = false;
        /*
        public void LoadThumbnail()
        {
            _thumbnail = BitmapImageFactory.GetBitmap();
            var stream = Base.GetThumbnail();
            if (stream != null)
            {
                _thumbnail.SetSource(stream);
            }
        }*/

        //private WriteableBitmap _thumbnail = null;
        /// <summary>
        /// Album art thumbnail
        /// </summary>
        public BitmapImage Thumbnail
        {
            get
            {
                if (_thumbnailChecked == false)
                {
                    try
                    {
                        _thumbnail = new BitmapImage();

                        var stream = Base.GetThumbnail();
                        if (stream != null)
                        {
                            _thumbnail.SetSource(stream);
                        }
                        _thumbnailChecked = true;
                    }
                    catch (Exception ex)
                    {
                        _thumbnailChecked = true;
                    }
                }
                /*
                if (_thumbnailChecked == false)
                {
                    try
                    {
                        //_thumbnail = new BitmapImage();
                        var wrB = new WriteableBitmap(64, 64);
                        var stream = Base.GetAlbumArt();
                        if (stream != null)
                        {
                            wrB.SetSource(stream);
                            //_thumbnail.SetSource(stream);
                        }
                        _thumbnailChecked = true;
                        
                        wrB.Dispatcher.BeginInvoke(delegate()
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                wrB.SaveJpeg(ms, 64, 64, 0, 70);
                                BitmapImage bmp = new BitmapImage();
                                bmp.SetSource(ms);
                                _thumbnail = bmp;
                            }
                            //_thumbnail = new BitmapImage();
                            //wrB.
                        });
                        
                    }
                    catch (Exception ex)
                    {
                        _thumbnailChecked = true;
                    }
                }
                */
                return _thumbnail;
            }
        }
        
    }

}
