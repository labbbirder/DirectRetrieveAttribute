using System;

namespace com.bbbirder.DirectAttribute{
    public class WeakHolder<T> where T :class{
        WeakReference<T> wr;
        Func<T> creator;
        public T Value {
            get {
                if(!wr.TryGetTarget(out var target)){
                    wr.SetTarget(target = creator());
                }
                return target;
            }
        }
        public WeakHolder(Func<T> creator){
            this.creator = creator;
            this.wr = new WeakReference<T>(creator());
        }

    }
}