<script >
  import { reportError } from './utils/error-reporter.js'
  // #ifdef APP-ANDROID || APP-HARMONY
	let firstBackTime = 0
  // #endif
	export default {
		// App 级未捕获异常（脚本错误、未处理的异步异常），统一上报
		onError: function (error) {
			reportError('App', error)
		},
		onLaunch: function () {
			console.log('App Launch')
		},
		onShow: function () {
			console.log('App Show')
		},
		onHide: function () {
			console.log('App Hide')
		},
		// #ifdef APP-ANDROID || APP-HARMONY
		onLastPageBackPress: function () {
			console.log('App LastPageBackPress')
			if (firstBackTime == 0) {
				uni.showToast({
					title: '再按一次退出应用',
					position: 'bottom',
				})
				firstBackTime = Date.now()
				setTimeout(() => {
					firstBackTime = 0
				}, 2000)
			} else if (Date.now() - firstBackTime < 2000) {
				firstBackTime = Date.now()
				uni.exit()
			}
		},
		// #endif
		onExit: function () {
			console.log('App Exit')
		},
	}
</script>

<style>
	/*每个页面公共css */
	.uni-row {
		flex-direction: row;
	}

	.uni-column {
		flex-direction: column;
	}
</style>