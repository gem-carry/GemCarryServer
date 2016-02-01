using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace GemCarryServer
{
    /// <summary>
    /// Represents a collection of reusable SocketAsyncEventArgs objects
    /// </summary>
    public class SocketAsyncEventArgsPool
    {
        private Stack<SocketAsyncEventArgs> m_pool;

        /// <summary>
        /// Initializes pool to the maximum size provided
        /// </summary>
        /// <param name="capacity"></param>
        public SocketAsyncEventArgsPool(int capacity)
        {
            m_pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        /// <summary>
        /// Add a SocketAsyncEventAg instance to the pool
        /// </summary>
        /// <param name="item"></param>
        public void Push(SocketAsyncEventArgs item)
        {
            if (null == item) { throw new ArgumentNullException("Cannot add a null item to SocketAsyncEventArgsPool"); }
            lock(m_pool)
            {
                m_pool.Push(item);
            }
        }

        /// <summary>
        /// Removes a SocketAsyncEventArgs instance from the pool
        /// and returns the object removed from the pool
        /// </summary>
        /// <returns></returns>
        public SocketAsyncEventArgs Pop()
        {
            lock(m_pool)
            {
                return m_pool.Pop();
            }
        }

        /// <summary>
        /// Returns number of SocketAsyncEventArgs instance in the pool
        /// </summary>
        public int Count
        {
            get { return m_pool.Count; }
        }

    }
}
