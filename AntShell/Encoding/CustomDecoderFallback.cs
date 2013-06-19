// /********************************************************
//  *   
//  * CONFIDENTIAL  
//  * 
//  * ---  
//  * 
//  *  (c) 2010-2013 Ant Micro <antmicro.com>  
//  *  (c) 2011-2013 Realtime Embedded <rte.se>
//  *  All Rights Reserved.
//  * 
//  * NOTICE:  All information contained herein is, and remains    
//  * the property of Ant Micro and Realtime Embedded. 
//  * The intellectual and technical  concepts contained 
//  * herein are proprietary to Ant Micro and  are protected 
//  * by trade secret or copyright law.
//  * Dissemination of this information or reproduction of this material
//  * is strictly forbidden unless prior written permission is obtained
//  * from Ant Micro and Realtime Embedded.
//  *
//  */
using System;
using System.Text;

namespace AntShell.Encoding
{
    public class CustomDecoderFallback : DecoderFallback
    {
        public CustomDecoderFallback()
        {
        }

        private CustomDecoderFallbackBuffer buffer;

        public bool IsError { get { return buffer != null ? buffer.IsError : false; } }

        #region implemented abstract members of DecoderFallback

        public override DecoderFallbackBuffer CreateFallbackBuffer()
        {
            if (buffer == null)
            {
                buffer = new CustomDecoderFallbackBuffer();
            }

            return buffer;
        }

        public override int MaxCharCount { get { return 0; } }

        #endregion
    }
}

