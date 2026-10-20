document.addEventListener('DOMContentLoaded', function () {
    const popup = document.getElementById('copy-popup');
    const copyButton = document.getElementById('copy-button');

    let cursorX = 0, cursorY = 0;
    let isContextMenuVisible = false;

    let selectedContent = "";
    let selectedElement = null; 

    let isDragging = false;
    let draggedElement = null;
	
	let testMSG = "not send";
	
	var windowWidth = window.innerWidth;
	var windowHeight = window.innerHeight;

    // |------------------------------------------------------
    // | Control Functions
    // |------------------------------------------------------

    function showContextMenu(x, y) {
        if (selectedContent !== '') {
            popup.style.left = `${x}px`;
            popup.style.top = `${y}px`;
            popup.style.display = 'block';
            isContextMenuVisible = true;
        }
    }

    function hideContextMenu() {
        popup.style.display = 'none';
        isContextMenuVisible = false;
    }

    function selectContent(x, y) {
        if (isContextMenuVisible) return;
        deselectContent();
    
        const element = document.elementFromPoint(x, y);
        if (element && element.tagName === 'SPAN') {
            element.style.backgroundColor = 'rgba(200, 200, 200, 0.5)';
            selectedContent = element.textContent.trim();
            selectedElement = element;
            copyButton.innerHTML = "Copy Text";
        } else if (element && element.tagName === 'IMG') {
            element.style.boxShadow = '0px 0px 10px 2px rgba(200, 200, 200, 1)';
            selectedContent = "IMGURL_" + element.getAttribute("src");
            selectedElement = element;
            copyButton.innerHTML = "Copy Image";
        } else {
            selectedContent = "";
            selectedElement = null;
        }
		
		
            
        cursorX = x;
        cursorY = y;
    }
	
	//for VR click to select
	/*function selectContent(x, y) {
        //if (isContextMenuVisible) return;
        //deselectContent();
    
        const element = document.elementFromPoint(x, y);
        if (element && element.tagName === 'SPAN') {
            element.style.backgroundColor = 'rgba(200, 200, 200, 0.5)';
			if(selectedContent != element.textContent.trim()){
				deselectContent();
				selectedContent = element.textContent.trim();
				selectedElement = element;
			}
            copyButton.innerHTML = "Copy Text";
        } else if (element && element.tagName === 'IMG') {
            element.style.boxShadow = '0px 0px 10px 2px rgba(200, 200, 200, 1)';
			if(selectedContent != "IMGURL_" + element.getAttribute("src")){
				deselectContent();
				selectedContent = "IMGURL_" + element.getAttribute("src");
				selectedElement = element;
			}
            copyButton.innerHTML = "Copy Image";
        } else {
            selectedContent = "";
            selectedElement = null;
        }
            
        cursorX = x;
        cursorY = y;
    }*/

    function deselectContent() {
        if (isContextMenuVisible) return;

        if (selectedElement && selectedElement.tagName === 'IMG') {
            selectedElement.style.boxShadow = '';  // Remove the shadow or border
        }
        else if (selectedElement && selectedElement.tagName === 'SPAN') {
            selectedElement.style.backgroundColor = '';  // Remove the background color
        }
    }
	
	//for VR click to copy
	/*function deselectContent() {
        //if (isContextMenuVisible) return;

        if (selectedElement && selectedElement.tagName === 'IMG') {
            selectedElement.style.boxShadow = '';  // Remove the shadow or border
        }
        else if (selectedElement && selectedElement.tagName === 'SPAN') {
            selectedElement.style.backgroundColor = '';  // Remove the background color
        }
    }*/
    
    function copySelectionToClipboard() {
        fetch('/api/clipboard', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ clipboard: selectedContent })
        })
        .then(response => response.json())
        .then(data => {
            if(data.status === "success") {
                
                getClipboardContent();
            } else {
                console.error('Failed to copy content:', data.message);
            }
        })
        .catch(err => {
            console.error('Failed to copy content:', err);
        });
    }

    function getClipboardContent() {
        fetch('/api/clipboard', { method: 'GET' })
        .then(response => response.json())
        .then(data => console.log('Clipboard Content:', data.clipboard))
        .catch(err => console.error('Failed to read clipboard content:', err));
    }

    function startDragging(x, y) {
        if (selectedContent === "" || isDragging || isContextMenuVisible) return;

        draggedElement = document.createElement('div');
        draggedElement.style.position = 'fixed';
        draggedElement.style.opacity = '0.5';
        draggedElement.style.pointerEvents = 'none'; // So that it doesn't interfere with mouse events
    
        if (selectedContent.startsWith('IMGURL_')) {
            const imgURL = selectedContent.replace('IMGURL_', '');
            const img = document.createElement('img');
            img.src = imgURL;
            img.style.width = '200px';
            draggedElement.appendChild(img);
        } else {
            draggedElement.innerHTML = selectedContent;
            draggedElement.style.whiteSpace = 'nowrap';
        }
    
        draggedElement.style.left = `${x}px`;
        draggedElement.style.top = `${y}px`;
        document.body.appendChild(draggedElement);

        fetch('/api/drag', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ drag_content: selectedContent, is_dragging: true })  // Send dragging status
        })
        .then(response => response.json())
        .then(data => {
            if (data.status === 'success') {
                
            } else {
                console.error('Failed to update drag content:', data.message);
            }
        })
        .catch(err => {
            console.error('Failed to update drag content:', err);
        });

        isDragging = true;
    }

    function updateDraggedElementPosition(x, y) {
		
        if (draggedElement) {
            draggedElement.style.left = `${x}px`;
            draggedElement.style.top = `${y}px`;
        }
    }

    function removeDraggedElement() {
        if (draggedElement) {
            document.body.removeChild(draggedElement);
            draggedElement = null;
        }
    }

    function stopDragging() {
        removeDraggedElement();
        
        isDragging = false;

        fetch('/api/drag', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ drag_content: "", is_dragging: false })  // Clear dragging status
        })
        .then(response => response.json())
        .then(data => {
            if (data.status === 'success') {
                
            } else {
                console.error('Failed to clear drag content:', data.message);
            }
        })
        .catch(err => {
            console.error('Failed to clear drag content:', err);
        });
    }

    checkDragIntervalId = setInterval(checkServerDragging, 1000);
    async function checkServerDragging() {
        try {
            const response = await fetch('/api/drag_status'); // Query the shared drag state.
            const data = await response.json();
            isDragging = data.is_dragging; // The response contains the boolean field is_dragging.
            if (!isDragging) stopDragging();
        } catch (error) {
            console.error('Failed to check server dragging status', error);
        }
    }


    // |------------------------------------------------------
    // | Event Listeners For Keyboard/Mouse Control
    // |------------------------------------------------------
    
    // Mouse Hover For Selection
    /*document.addEventListener('mousemove', function (event) {
        if (!isDragging) selectContent(event.clientX, event.clientY);
        else updateDraggedElementPosition(event.clientX, event.clientY);
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'c') {
            if (isDragging) return;
            showContextMenu(cursorX, cursorY);
        }
        if (event.key === 'Escape') hideContextMenu();
        if (event.key === 'd') isDragging ? stopDragging() : startDragging(cursorX, cursorY);
    });

    // Click For Copying/Canceling Selection
    document.addEventListener('click', function (event) {
        if (isContextMenuVisible) {
            if (!popup.contains(event.target) && event.target !== popup) {
                hideContextMenu();
            }
            else {
                copySelectionToClipboard();
                hideContextMenu();
            }
        }
    });*/
	
	
	//for interaction in VR
	window.onHandPadMessage( function(message) {
		arrayOfMessage = message.data.split(",");
		
		data = {'x': parseFloat(arrayOfMessage[0]),'y': 1-parseFloat(arrayOfMessage[1]), 'drag': parseInt(arrayOfMessage[2]), 'copy': parseInt(arrayOfMessage[3])}
		//testMSG = data.x + "," + data.y + "," + data.menu + "," + data.copy;
		
		if(data.x >= 0 && data.x <= 1 && data.y >= 0 && data.y <= 1){
			
			if (!isDragging) selectContent(data.x * windowWidth, data.y * windowHeight);
			else updateDraggedElementPosition(data.x * windowWidth, data.y * windowHeight);
		}
		
		if(data.drag == 1){
			isDragging ? stopDragging() : startDragging(data.x * windowWidth, data.y * windowHeight);
		}

		if(data.copy == 1){
			if (isDragging) return;
			
			copySelectionToClipboard();
			hideContextMenu();
		}
	})
});
