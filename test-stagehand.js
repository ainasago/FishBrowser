const { Stagehand } = require('@browserbasehq/stagehand');

(async () => {
    console.log('Creating Stagehand instance...');
    const stagehand = new Stagehand({
        env: 'LOCAL',
        verbose: 2,
        debugDom: true
    });
    
    try {
        console.log('Calling stagehand.init()...');
        await stagehand.init();
        
        console.log('Init completed. Checking page object...');
        console.log('stagehand.context:', stagehand.context);
        
        // Stagehand 3.x uses context.pages() instead of page
        const page = stagehand.context.pages()[0];
        console.log('page:', page);
        console.log('typeof page:', typeof page);
        
        if (page) {
            console.log('✅ Page object exists!');
            console.log('Navigating to GitHub...');
            await page.goto('https://github.com');
            console.log('✅ Navigation successful!');
        } else {
            console.error('❌ Page object is undefined after init()');
        }
        
    } catch (error) {
        console.error('❌ Error:', error);
        console.error('Stack:', error.stack);
    } finally {
        console.log('Closing stagehand...');
        await stagehand.close();
        console.log('Done!');
    }
})();
