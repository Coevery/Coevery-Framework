﻿using System;
using System.Runtime.Remoting.Messaging;
using System.Web;

namespace Orchard.Environment.State {

    /// <summary>
    /// Holds some state for the current HttpContext or thread
    /// </summary>
    /// <typeparam name="T">The type of data to store</typeparam>
    public class ContextState<T> where T : class {
        private readonly string _name;
        private readonly Func<T> _defaultValue;

        public ContextState(string name) {
            _name = name;
        }

        public ContextState(string name, Func<T> defaultValue) {
            _name = name;
            _defaultValue = defaultValue;
        }

        public T GetState() {
            if (HttpContext.Current == null) {
                var data = CallContext.GetData(_name);

                if (data == null) {
                    if (_defaultValue != null) {
                        CallContext.SetData(_name, data = _defaultValue());
                        return data as T;
                    }
                }

                return data as T;
            }

            if (HttpContext.Current.Items[_name] == null) {
                HttpContext.Current.Items[_name] = _defaultValue == null ? null : _defaultValue();
            }

            return HttpContext.Current.Items[_name] as T;

        }

        public void SetState(T state) {
            if (HttpContext.Current == null) {
                CallContext.SetData(_name, state);
            }
            else {
                HttpContext.Current.Items[_name] = state;
            }
        }
    }
}

