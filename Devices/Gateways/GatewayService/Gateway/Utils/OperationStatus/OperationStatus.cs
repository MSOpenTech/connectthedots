//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

namespace Microsoft.ConnectTheDots.Gateway
{
    using System.Diagnostics;

    //--//

    [DebuggerDisplay( "OperationCode = {OperationCode}" )]
    public sealed class OperationStatus
    {
        internal OperationStatus( ) { }

        public ErrorCode OperationCode { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSuccess
        {
            get { return OperationCode == ErrorCode.Success; }
        }
    }

    [DebuggerDisplay( "OperationCode = {OperationCode}, Result = {Result}" )]
    public sealed class OperationStatus<T>
    {
        internal OperationStatus( ) { }

        public T Result { get; set; }

        public ErrorCode OperationCode { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSuccess
        {
            get { return OperationCode == ErrorCode.Success; }
        }
    }
}