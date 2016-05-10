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
    using System;

    //--//

    public static class OperationStatusFactory
    {
        private static readonly OperationStatus _successConst = new OperationStatus { OperationCode = ErrorCode.Success };

        //--//

        public static OperationStatus CreateSuccess( )
        {
            return _successConst;
        }

        public static OperationStatus<T> CreateSuccess<T>( T result )
        {
            return new OperationStatus<T> { OperationCode = ErrorCode.Success, Result = result };
        }

        public static OperationStatus CreateError( ErrorCode errorCode )
        {
            return new OperationStatus { OperationCode = errorCode };
        }
        
        public static OperationStatus CreateError( ErrorCode errorCode, string errorMessage )
        {
            return new OperationStatus { OperationCode = errorCode, ErrorMessage = errorMessage };
        }

        public static OperationStatus CreateError( ErrorCode errorCode, Exception exception )
        {
            return new OperationStatus { OperationCode = errorCode, ErrorMessage = exception.Message + exception.StackTrace };
        }

        public static OperationStatus<T> CreateError<T>( ErrorCode errorCode )
        {
            return new OperationStatus<T> { OperationCode = errorCode, Result = default( T ) };
        }

        public static OperationStatus<T> CreateError<T>( ErrorCode errorCode, Exception exception )
        {
            return new OperationStatus<T> { OperationCode = errorCode, ErrorMessage = exception.Message + exception.StackTrace, Result = default( T ) };
        }

        public static OperationStatus<T> CreateError<T>( ErrorCode errorCode, string errorMessage, T result = default (T) )
        {
            return new OperationStatus<T> { OperationCode = errorCode, ErrorMessage = errorMessage, Result = result };
        }

        public static OperationStatus<T> CreateError<T>( ErrorCode errorCode, string errorMessage, Exception exception )
        {
            return new OperationStatus<T> { OperationCode = errorCode, ErrorMessage = errorMessage + exception.Message + exception.StackTrace, Result = default( T ) };
        }

        public static OperationStatus CopyFrom( OperationStatus source )
        {
            return new OperationStatus { OperationCode = source.OperationCode, ErrorMessage = source.ErrorMessage };
        }

        public static OperationStatus CopyFrom<T>( OperationStatus<T> source )
        {
            return new OperationStatus { OperationCode = source.OperationCode, ErrorMessage = source.ErrorMessage };
        }

        public static OperationStatus<TOut> CopyFrom<TOut>( OperationStatus source )
        {
            return new OperationStatus<TOut> { OperationCode = source.OperationCode, ErrorMessage = source.ErrorMessage, Result = default( TOut ) };
        }

        public static OperationStatus<TOut> CopyFrom<TIn, TOut>( OperationStatus<TIn> source, TOut result = default (TOut) )
        {
            return new OperationStatus<TOut> { OperationCode = source.OperationCode, ErrorMessage = source.ErrorMessage, Result = result };
        }
    }
}