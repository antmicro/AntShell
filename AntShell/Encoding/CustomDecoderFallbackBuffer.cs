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
    public class CustomDecoderFallbackBuffer : DecoderFallbackBuffer
    {
        public CustomDecoderFallbackBuffer()
        {
        }

        private bool _IsError;
        public bool IsError 
        { 
            get
            {
                var result = _IsError;
                _IsError = false;
                return result;
            }

            private set
            {
                _IsError = value;
            }
        }

        private char? Replacement 
        {
            get; set;
        }

        #region implemented abstract members of DecoderFallbackBuffer

        public override bool Fallback(byte[] bytesUnknown, int index)
        {
            IsError = true;
            Replacement = '?';
            return true;
        }

        public override char GetNextChar()
        {
            var result = Replacement;
            Replacement = null;
            return result ?? (char)0;
        }

        public override bool MovePrevious()
        {
            return false;
        }

        public override int Remaining { get { return Replacement.HasValue ? 1 : 0; } }

        #endregion
    }
}

