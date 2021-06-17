using System;
using System.Collections.Generic;
using System.Text;

namespace MT.MiniIoc.Core {
    /// <summary>
    /// 
    /// </summary>
    public interface IMIocContainter {
        /// <summary>
        /// 注册服务（每次请求都为新对象）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IMIocContainter RegisterScope<T>()
          where T : class;
        /// <summary>
        /// 注册服务（每次请求都为新对象）
        /// </summary>
        /// <param name="classType">服务类型</param>
        /// <returns></returns>
        IMIocContainter RegisterScope(Type classType);
        /// <summary>
        /// 注册服务（每次请求都为新对象）
        /// </summary>
        /// <typeparam name="TI">服务接口</typeparam>
        /// <typeparam name="T">服务实现类</typeparam>
        /// <returns></returns>
        IMIocContainter RegisterScope<TI, T>()
           where TI : class
           where T : class, TI;

        /// <summary>
        /// 注册单例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="createImmediately">立即创建对象</param>
        /// <param name="key">为该实例添加key</param>
        /// <returns></returns>
        IMIocContainter RegisterSingle<T>(bool createImmediately, string key = null)
            where T : class;
        /// <summary>
        /// 注册单例
        /// </summary>
        /// <param name="classType">服务类型</param>
        /// <param name="createImmediately">立即创建对象</param>
        /// <param name="key">为该实例添加key</param>
        /// <returns></returns>
        IMIocContainter RegisterSingle(Type classType, bool createImmediately, string key = null);

        /// <summary>
        /// 注册单例
        /// </summary>
        /// <typeparam name="TI">服务接口</typeparam>
        /// <typeparam name="T">服务实现类</typeparam>
        /// <param name="createImmediately">立即创建对象</param>
        /// <param name="key">为该实例添加key</param>
        /// <returns></returns>
        IMIocContainter RegisterSingle<TI, T>(bool createImmediately, string key = null)
          where TI : class
          where T : class, TI;
        /// <summary>
        /// 注册单例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="factory">创建该实例的委托</param>
        /// <param name="key">为该实例添加key</param>
        /// <returns></returns>
        IMIocContainter RegisterSingle<T>(Func<T> factory, string key = null)
          where T : class;

        /// <summary>
        /// 注销实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">该实例的key</param>
        void UnRegister<T>(string key = null);
    }
}
