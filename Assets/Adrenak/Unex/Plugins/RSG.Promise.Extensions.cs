namespace RSG {
	public static class RSGPromiseExtensions {
		public static Promise<T> Default<T>(this Promise<T> promise) {
			promise.Resolve(default(T));
			return promise;
		}
	}
}
