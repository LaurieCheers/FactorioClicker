using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FactorioClicker
{
    public interface Notifiable<T> where T:Notification
    {
        void Notify(T notification);
    }

    public interface NotifyRule
    {
        void Notify(Notification notification);
    }

    public class NotifyRule<T> : NotifyRule where T : Notification
    {
        WeakReference reference;

        public NotifyRule(Notifiable<T> n)
        {
            reference = new WeakReference(n);
        }

        public void Notify(Notification notification)
        {
            if (reference.IsAlive)
            {
                ((Notifiable<T>)reference.Target).Notify((T)notification);
            }
        }
    }

    public class Notification
    {
    }

    public class NotificationManager
    {
        public static NotificationManager instance = new NotificationManager();

        Dictionary<Type, List<NotifyRule>> notificationRules = new Dictionary<Type, List<NotifyRule>>();

        public void AddNotification<T>(Notifiable<T> target) where T:Notification
        {
            Type type = typeof(T);
            if (!notificationRules.ContainsKey(type))
            {
                notificationRules.Add(type, new List<NotifyRule>());
            }

            notificationRules[type].Add(new NotifyRule<T>(target));
        }

        public void Notify(Notification notification)
        {
            Type type = notification.GetType();
            if (notificationRules.ContainsKey(type))
            {
                foreach (NotifyRule n in notificationRules[type])
                {
                    n.Notify(notification);
                }
            }
        }
    }
}
