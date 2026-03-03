// ???????????????????????????????????????????????????????????????????
//  BIM Issue Manager ? APS Viewer + Issue Pins + CRUD
// ???????????????????????????????????????????????????????????????????

const IssueManager = (() => {
    let viewer = null;
    let issues = [];
    let selectedIssueId = null;
    let placementMode = false;
    let pinOverlay = null;
    let userRole = 'Viewer';

    // ── API base path ──
    const API = '/api/IssuesApi';

    // ── Status helpers ──
    const STATUS_LABELS = { 0: 'Open', 1: 'InProgress', 2: 'Done' };
    const STATUS_CSS = { 0: 'open', 1: 'inprogress', 2: 'done' };
    const TYPE_LABELS = { 0: 'Quality Defect', 1: 'Safety Deficiency', 2: 'Construction Defect', 3: 'Design Change' };

    // ── Location type constants ──
    // 0=Element, 1=Spatial, 2=ElementSpatial (combined)
    const LOCATION_LABELS = { 0: 'Element', 1: 'Spatial', 2: 'Element+Spatial' };

    // ── Role helpers ──
    const canEdit = () => userRole === 'Admin' || userRole === 'Manager';
    const canDelete = () => userRole === 'Admin';

    // ── INIT ──
    function init(urn, tokenEndpoint, role) {
        userRole = role || 'Viewer';
        // Hide "New Issue" button for read-only users
        const btn = document.getElementById('btnPlaceIssue');
        if (btn && !canEdit()) btn.style.display = 'none';
        initViewer(urn, tokenEndpoint || `${API}/GetViewerToken`);
    }

    // ??? APS VIEWER ???
    function initViewer(urn, tokenEndpoint) {
        const options = {
            env: 'AutodeskProduction2',
            api: 'streamingV2',
            getAccessToken: async (onSuccess) => {
                const resp = await fetch(tokenEndpoint, { method: 'POST' });
                const data = await resp.json();
                onSuccess(data.access_token, data.expires_in);
            }
        };

        Autodesk.Viewing.Initializer(options, () => {
            const container = document.getElementById('apsViewer');
            viewer = new Autodesk.Viewing.GuiViewer3D(container);
            viewer.start();

            const documentId = `urn:${urn}`;
            Autodesk.Viewing.Document.load(documentId, onDocumentLoaded, onDocumentFailed);
        });
    }

    function onDocumentLoaded(doc) {
        const viewable = doc.getRoot().getDefaultGeometry();
        viewer.loadDocumentNode(doc, viewable).then(() => {
            // Create pin overlay container
            pinOverlay = document.createElement('div');
            pinOverlay.id = 'pinOverlay';
            pinOverlay.style.cssText = 'position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none;';
            viewer.container.appendChild(pinOverlay);

            // Listen for camera changes to update pin positions
            viewer.addEventListener(Autodesk.Viewing.CAMERA_CHANGE_EVENT, updatePinPositions);

            // Click handler for placement mode
            viewer.container.addEventListener('click', onViewerClick);

            // Load issues
            loadIssues();
        });
    }

    function onDocumentFailed(errorCode, errorMsg) {
        console.error(`Failed to load document: ${errorCode} - ${errorMsg}`);
        document.getElementById('apsViewer').innerHTML =
            `<div class="d-flex align-items-center justify-content-center h-100">
                <div class="text-center text-muted">
                    <h4>Failed to load model</h4>
                    <p>Error: ${errorMsg || errorCode}</p>
                </div>
            </div>`;
    }

    // ??? PLACEMENT MODE ???
    function togglePlacementMode() {
        placementMode = !placementMode;
        const btn = document.getElementById('btnPlaceIssue');
        if (placementMode) {
            btn.classList.add('btn-danger');
            btn.classList.remove('btn-primary');
            btn.innerHTML = '<i class="bi bi-x-lg"></i> Cancel Placement';
            viewer.container.classList.add('placement-mode');
            showToast('Click on the 3D model to place an issue pin');
        } else {
            btn.classList.remove('btn-danger');
            btn.classList.add('btn-primary');
            btn.innerHTML = '<i class="bi bi-plus-lg"></i> New Issue';
            viewer.container.classList.remove('placement-mode');
        }
    }

    function onViewerClick(event) {
        if (!placementMode) return;

        const hitResult = viewer.impl.hitTest(
            event.clientX - viewer.container.getBoundingClientRect().left,
            event.clientY - viewer.container.getBoundingClientRect().top,
            false
        );

        if (!hitResult) return;

        placementMode = false;
        const btn = document.getElementById('btnPlaceIssue');
        btn.classList.remove('btn-danger');
        btn.classList.add('btn-primary');
        btn.innerHTML = '<i class="bi bi-plus-lg"></i> New Issue';
        viewer.container.classList.remove('placement-mode');

        // Determine location data
        const dbId = hitResult.dbId && hitResult.dbId > 0 ? hitResult.dbId : null;
        const worldPos = hitResult.intersectPoint;

        showCreateForm(dbId, worldPos);
    }

    // ??? ISSUES CRUD ???
    async function loadIssues() {
        const statusFilter = document.getElementById('filterStatus')?.value || '';
        const typeFilter = document.getElementById('filterType')?.value || '';

        let url = `${API}/GetAllIssues`;
        const params = new URLSearchParams();
        if (statusFilter) params.set('status', statusFilter);
        if (typeFilter) params.set('type', typeFilter);
        if (params.toString()) url += '?' + params.toString();

        const resp = await fetch(url);
        issues = await resp.json();

        renderIssueList();
        renderPins();
    }

    async function createIssue(formData) {
        const resp = await fetch(`${API}/CreateIssue`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(formData)
        });
        const newIssue = await resp.json();
        await loadIssues();
        selectIssue(newIssue.value.id);
    }

    async function changeStatus(issueId, newStatus) {
        await fetch(`${API}/ChangeIssueStatus/${issueId}/status`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ newStatus, changedBy: null, comment: null })
        });
        await loadIssues();
        selectIssue(issueId);
    }

    async function deleteIssue(issueId) {
        if (!confirm('Delete this issue permanently?')) return;
        await fetch(`${API}/DeleteIssue/${issueId}`, {
            method: 'DELETE'
        });
        selectedIssueId = null;
        await loadIssues();
        showListView();
    }

    async function updateIssue(issueId, formData) {
        const resp = await fetch(`${API}/UpdateIssue/${issueId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(formData)
        });
        if (!resp.ok) {
            const body = await resp.json().catch(() => null);
            const msg = body?.error?.name || body?.error || 'Failed to update issue.';
            alert(msg);
            return;
        }
        await loadIssues();
        selectIssue(issueId);
    }

    async function removePhoto(issueId, photoId) {
        if (!confirm('Remove this photo permanently?')) return;
        const resp = await fetch(`${API}/RemovePhoto/${issueId}/${photoId}`, {
            method: 'DELETE'
        });
        if (!resp.ok) {
            const body = await resp.json().catch(() => null);
            const msg = body?.error?.name || body?.error || 'Failed to remove photo.';
            alert(msg);
            return;
        }
        selectIssue(issueId);
    }

    async function uploadPhoto(issueId, file, correctionStage) {
        // Clear any previous upload error for this stage
        const errorId = correctionStage === 0 ? 'beforePhotoError' : 'afterPhotoError';
        const errorEl = document.getElementById(errorId);
        if (errorEl) { errorEl.style.display = 'none'; errorEl.textContent = ''; }

        const form = new FormData();
        form.append('file', file);
        form.append('correctionStage', correctionStage);

        const resp = await fetch(`${API}/UploadPhoto/${issueId}`, {
            method: 'POST',
            body: form
        });

        if (!resp.ok) {
            try {
                const body = await resp.json();
                console.log(body);
                const msg = body?.error?.description || body?.error || 'Failed to upload photo.';
                if (errorEl) { 
                    errorEl.textContent = msg.name;
                    errorEl.style.display = 'block';
                }
            } catch {
                if (errorEl) {
                    errorEl.textContent = 'Failed to upload photo.';
                    errorEl.style.display = 'block';
                }
            }
            return;
        }

        selectIssue(issueId);
    }

    // ??? ISSUE DETAIL ???
    async function selectIssue(issueId) {
        selectedIssueId = issueId;
        console.log(selectedIssueId);
        const resp = await fetch(`${API}/GetIssueById/${issueId}`);
        if (!resp.ok) return;
        const issue = await resp.json();

        renderIssueDetail(issue);

        // Highlight in list
        document.querySelectorAll('.issue-list-item').forEach(el => {
            el.classList.toggle('active', el.dataset.id === issueId);
        });

        // Navigate viewer to the issue location
        navigateToIssue(issue);
    }

    function navigateToIssue(issue) {
        if (!viewer) return;

        const loc = issue.location;

        // If element-based, isolate and fit to the element
        if (loc.dbId) {
            viewer.select([loc.dbId]);
            viewer.fitToView([loc.dbId]);
        }

        // If has world position, also set camera target
        if (loc.worldPosition) {
            const pos = new THREE.Vector3(loc.worldPosition.x, loc.worldPosition.y, loc.worldPosition.z);
            const navTarget = pos.clone();
            viewer.navigation.setTarget(navTarget);
        }
    }

    // ??? PIN RENDERING ???
    function renderPins() {
        if (!pinOverlay) return;
        pinOverlay.innerHTML = '';

        issues.forEach(issue => {
            const loc = issue.location;
            if (!loc.worldPosition && !loc.dbId) return;

            const pin = document.createElement('div');
            pin.className = `issue-pin pin-${STATUS_CSS[issue.status]}`;
            pin.style.pointerEvents = 'auto';
            pin.dataset.issueId = issue.id;
            pin.title = issue.title;
            pin.textContent = TYPE_LABELS[issue.type]?.[0] || '?';

            pin.addEventListener('click', (e) => {
                e.stopPropagation();
                selectIssue(issue.id);
            });

            pinOverlay.appendChild(pin);
        });

        updatePinPositions();
    }

    function updatePinPositions() {
        if (!viewer || !pinOverlay) return;

        pinOverlay.querySelectorAll('.issue-pin').forEach(pin => {
            const issue = issues.find(i => i.id === pin.dataset.issueId);
            if (!issue) { pin.style.display = 'none'; return; }

            let worldPoint = null;
            const loc = issue.location;

            if (loc.worldPosition) {
                worldPoint = new THREE.Vector3(loc.worldPosition.x, loc.worldPosition.y, loc.worldPosition.z);
            } else if (loc.dbId) {
                // Get bounding box center for element-based issues
                const tree = viewer.model?.getInstanceTree();
                if (tree) {
                    const box = new THREE.Box3();
                    tree.enumNodeFragments(loc.dbId, (fragId) => {
                        const fragBox = new THREE.Box3();
                        viewer.model.getFragmentList().getWorldBounds(fragId, fragBox);
                        box.union(fragBox);
                    }, true);
                    if (!box.isEmpty()) {
                        worldPoint = box.getCenter(new THREE.Vector3());
                    }
                }
            }

            if (!worldPoint) { pin.style.display = 'none'; return; }

            const screenPos = viewer.worldToClient(worldPoint);
            if (!screenPos) { pin.style.display = 'none'; return; }

            pin.style.display = 'flex';
            pin.style.left = screenPos.x + 'px';
            pin.style.top = screenPos.y + 'px';
        });
    }

    // ??? UI RENDERING ???
    function renderIssueList() {
        const list = document.getElementById('issueList');
        if (!list) return;

        if (issues.length === 0) {
            list.innerHTML = `
                <div class="text-center text-muted py-5">
                    <p>No issues found.</p>
                    <p class="small">Click "New Issue" and place a pin on the 3D model.</p>
                </div>`;
            return;
        }

        list.innerHTML = issues.map(issue => `
            <div class="issue-list-item ${issue.id === selectedIssueId ? 'active' : ''}"
                 data-id="${issue.id}" onclick="IssueManager.selectIssue('${issue.id}')">
                <div class="d-flex justify-content-between align-items-start">
                    <span class="issue-title">${escapeHtml(issue.title)}</span>
                    <span class="badge badge-${STATUS_CSS[issue.status]}">${STATUS_LABELS[issue.status]}</span>
                </div>
                <div class="issue-meta">
                    ${TYPE_LABELS[issue.type]} &middot;
                    ${LOCATION_LABELS[issue.location.locationType] || 'Unknown'} ${issue.location.dbId ? '#' + issue.location.dbId : ''} &middot;
                    ${new Date(issue.createdAt).toLocaleDateString()}
                </div>
            </div>
        `).join('');
    }

    function renderIssueDetail(issue) {
        const body = document.getElementById('panelBody');
        if (!body) return;

        const loc = issue.location;
        let locationText = LOCATION_LABELS[loc.locationType] || 'Unknown';
        if (loc.dbId) locationText += ` – dbId: ${loc.dbId}`;
        if (loc.worldPosition) locationText += ` – World: (${loc.worldPosition.x?.toFixed(2)}, ${loc.worldPosition.y?.toFixed(2)}, ${loc.worldPosition.z?.toFixed(2)})`;

        const beforePhotos = issue.photos.filter(p => p.correctionStage === 0);
        const afterPhotos = issue.photos.filter(p => p.correctionStage === 1);

        const renderPhotoGrid = (photos, issueId) => {
            console.log(photos.map(a=>a.presignedUrl))
            if (photos.length === 0) return '<p class="small text-muted">No photos yet.</p>';
            return `<div class="photo-grid">
                ${photos.map(p => `
                    <div class="photo-item">
                        <img src="${p.presignedUrl || ''}" alt="${escapeHtml(p.fileName)}" title="${escapeHtml(p.fileName)}" />
                        ${canDelete() ? `<button class="photo-delete-btn" title="Remove photo"
                            onclick="event.stopPropagation(); IssueManager.removePhoto('${issueId}', '${p.id}')">
                            <i class="bi bi-trash"></i>
                        </button>` : ''}
                    </div>
                `).join('')}
            </div>`;
        };

        body.innerHTML = `
            <div class="issue-detail">
                <div class="d-flex justify-content-between align-items-center mb-2">
                    <button class="btn btn-sm btn-outline-secondary" onclick="IssueManager.showListView()">
                        &larr; Back
                    </button>
                    <div class="d-flex gap-1">
                        ${canEdit() ? `<button class="btn btn-sm btn-outline-primary" onclick="IssueManager.showEditForm('${issue.id}')">
                            <i class="bi bi-pencil"></i> Edit
                        </button>` : ''}
                        ${canDelete() ? `<button class="btn btn-sm btn-outline-danger" onclick="IssueManager.deleteIssue('${issue.id}')">
                            <i class="bi bi-trash"></i> Delete
                        </button>` : ''}
                    </div>
                </div>

                <h5 class="mb-1">${escapeHtml(issue.title)}</h5>
                <div class="mb-3">
                    <span class="badge badge-${STATUS_CSS[issue.status]}">${STATUS_LABELS[issue.status]}</span>
                    <span class="badge bg-secondary">${TYPE_LABELS[issue.type]}</span>
                </div>

                <h6>Location</h6>
                <p class="small mb-1">${locationText}</p>

                <h6>Description</h6>
                <p class="description-text">${escapeHtml(issue.description)}</p>

                <h6>Status</h6>
                ${canEdit() ? `
                <div class="btn-group btn-group-sm mb-3" role="group">
                    <button class="btn ${issue.status === 0 ? 'btn-danger' : 'btn-outline-danger'}"
                            onclick="IssueManager.changeStatus('${issue.id}', 0)">Open</button>
                    <button class="btn ${issue.status === 1 ? 'btn-warning' : 'btn-outline-warning'}"
                            onclick="IssueManager.changeStatus('${issue.id}', 1)">In Progress</button>
                    <button class="btn ${issue.status === 2 ? 'btn-success' : 'btn-outline-success'}"
                            onclick="IssueManager.changeStatus('${issue.id}', 2)">Done</button>
                </div>
                ` : `<p class="small"><span class="badge badge-${STATUS_CSS[issue.status]}">${STATUS_LABELS[issue.status]}</span></p>`}

                <h6>Photos – Before Correction</h6>
                ${renderPhotoGrid(beforePhotos, issue.id)}
                ${canEdit() ? `
                <div class="mt-2">
                    <input type="file" accept="image/*" class="form-control form-control-sm"
                           onchange="IssueManager.uploadPhoto('${issue.id}', this.files[0], 0)" />
                    <div id="beforePhotoError" class="text-danger small mt-1" style="display:none;"></div>
                </div>
                ` : ''}

                <h6>Photos – After Correction</h6>
                ${renderPhotoGrid(afterPhotos, issue.id)}
                ${canEdit() ? `
                <div class="mt-2">
                    <input type="file" accept="image/*" class="form-control form-control-sm"
                           ${issue.status !== 2 ? 'disabled' : ''}
                           onchange="IssueManager.uploadPhoto('${issue.id}', this.files[0], 1)" />
                    ${issue.status !== 2 ? '<small class="text-muted d-block mt-1">After-correction photos can only be added when the issue is Done.</small>' : ''}
                    <div id="afterPhotoError" class="text-danger small mt-1" style="display:none;"></div>
                </div>
                ` : ''}

                <h6>Status History</h6>
                <ul class="status-history">
                    ${[...issue.statusHistory]
                        .sort((a, b) => new Date(b.updatedOn) - new Date(a.updatedOn))
                        .map((h, idx, arr) => {
                        const prevEntry = idx < arr.length - 1 ? arr[idx + 1] : null;
                        const isCreation = !prevEntry;
                        const fromStatus = prevEntry ? prevEntry.status : h.status;
                        const isSame = fromStatus === h.status;
                        return `
                        <li class="timeline-item">
                            <div class="timeline-dot dot-${STATUS_CSS[h.status]}"></div>
                            <div class="timeline-content">
                                <div>
                                    ${isCreation
                                        ? `<span class="badge badge-${STATUS_CSS[h.status]}">${STATUS_LABELS[h.status]}</span> <small class="text-muted">(created)</small>`
                                        : isSame
                                            ? `<span class="badge badge-${STATUS_CSS[h.status]}">${STATUS_LABELS[h.status]}</span>`
                                            : `<span class="badge badge-${STATUS_CSS[fromStatus]} me-1">${STATUS_LABELS[fromStatus]}</span>
                                               &rarr;
                                               <span class="badge badge-${STATUS_CSS[h.status]} ms-1">${STATUS_LABELS[h.status]}</span>`
                                    }
                                </div>
                                <small class="text-muted">${h.updatedBy ? h.updatedBy + ' &middot; ' : ''}${h.comment ? escapeHtml(h.comment) + ' &middot; ' : ''}${new Date(h.updatedOn).toLocaleString()}</small>
                            </div>
                        </li>`;
                    }).join('')}
                </ul>
            </div>`;
    }

    function showCreateForm(dbId, worldPos) {
        const body = document.getElementById('panelBody');
        if (!body) return;

        // 0=Element, 1=Spatial, 2=ElementSpatial
        let locationType;
        if (dbId && worldPos) {
            locationType = 2; // ElementSpatial ? both element and world position
        } else if (dbId) {
            locationType = 0; // Element only
        } else {
            locationType = 1; // Spatial only
        }

        body.innerHTML = `
            <div class="create-form">
                <div class="d-flex justify-content-between align-items-center mb-3">
                    <h6 class="mb-0">Create New Issue</h6>
                    <button class="btn btn-sm btn-outline-secondary" onclick="IssueManager.showListView()">Cancel</button>
                </div>

                <div class="mb-2">
                    <label class="form-label">Title</label>
                    <input type="text" id="newTitle" class="form-control form-control-sm" required />
                </div>

                <div class="mb-2">
                    <label class="form-label">Description</label>
                    <textarea id="newDescription" class="form-control form-control-sm" rows="3"></textarea>
                </div>

                <div class="mb-2">
                    <label class="form-label">Type</label>
                    <select id="newType" class="form-select form-select-sm">
                        <option value="0">Quality Defect</option>
                        <option value="1">Safety Deficiency</option>
                        <option value="2">Construction Defect</option>
                        <option value="3">Design Change</option>
                    </select>
                </div>

                <div class="mb-2">
                    <label class="form-label">Location <span class="badge bg-info">${LOCATION_LABELS[locationType]}</span></label>
                    <p class="small text-muted mb-1">
                        ${dbId ? `Element dbId: ${dbId}` : ''}
                        ${dbId && worldPos ? ' &middot; ' : ''}
                        ${worldPos ? `World: (${worldPos.x.toFixed(2)}, ${worldPos.y.toFixed(2)}, ${worldPos.z.toFixed(2)})` : ''}
                    </p>
                </div> 

                <button class="btn btn-primary btn-sm w-100" onclick="IssueManager.submitCreate(
                    ${locationType}, ${dbId || 'null'},
                    ${worldPos ? worldPos.x : 'null'}, ${worldPos ? worldPos.y : 'null'}, ${worldPos ? worldPos.z : 'null'}
                )">
                    Create Issue
                </button>
            </div>`;
    }

    function submitCreate(locationType, dbId, wx, wy, wz) {
        const title = document.getElementById('newTitle')?.value?.trim();
        const description = document.getElementById('newDescription')?.value?.trim();
        const type = parseInt(document.getElementById('newType')?.value || '0'); 

        if (!title) { alert('Title is required'); return; }
        if (!description) { alert('Description is required'); return; }

        createIssue({
            title,
            description,
            type,
            locationType,
            dbId,
            worldX: wx,
            worldY: wy,
            worldZ: wz 
        });
    }

    async function showEditForm(issueId) {
        const resp = await fetch(`${API}/GetIssueById/${issueId}`);
        if (!resp.ok) return;
        const issue = await resp.json();

        const loc = issue.location;
        const body = document.getElementById('panelBody');
        if (!body) return;

        body.innerHTML = `
            <div class="create-form">
                <div class="d-flex justify-content-between align-items-center mb-3">
                    <h6 class="mb-0">Edit Issue</h6>
                    <button class="btn btn-sm btn-outline-secondary" onclick="IssueManager.selectIssue('${issue.id}')">Cancel</button>
                </div>

                <div class="mb-2">
                    <label class="form-label">Title</label>
                    <input type="text" id="editTitle" class="form-control form-control-sm" value="${escapeHtml(issue.title)}" required />
                </div>

                <div class="mb-2">
                    <label class="form-label">Description</label>
                    <textarea id="editDescription" class="form-control form-control-sm" rows="3">${escapeHtml(issue.description)}</textarea>
                </div>

                <div class="mb-2">
                    <label class="form-label">Type</label>
                    <select id="editType" class="form-select form-select-sm">
                        <option value="0" ${issue.type === 0 ? 'selected' : ''}>Quality Defect</option>
                        <option value="1" ${issue.type === 1 ? 'selected' : ''}>Safety Deficiency</option>
                        <option value="2" ${issue.type === 2 ? 'selected' : ''}>Construction Defect</option>
                        <option value="3" ${issue.type === 3 ? 'selected' : ''}>Design Change</option>
                    </select>
                </div>

                <div class="mb-2">
                    <label class="form-label">Location <span class="badge bg-info">${LOCATION_LABELS[loc.locationType]}</span></label>
                    <p class="small text-muted mb-1">
                        ${loc.dbId ? `Element dbId: ${loc.dbId}` : ''}
                        ${loc.dbId && loc.worldPosition ? ' &middot; ' : ''}
                        ${loc.worldPosition ? `World: (${loc.worldPosition.x?.toFixed(2)}, ${loc.worldPosition.y?.toFixed(2)}, ${loc.worldPosition.z?.toFixed(2)})` : ''}
                    </p>
                    <small class="text-muted">Location cannot be changed after creation.</small>
                </div>

                <button class="btn btn-primary btn-sm w-100" onclick="IssueManager.submitUpdate(
                    '${issue.id}',
                    ${loc.locationType},
                    ${loc.dbId || 'null'},
                    ${loc.worldPosition ? loc.worldPosition.x : 'null'},
                    ${loc.worldPosition ? loc.worldPosition.y : 'null'},
                    ${loc.worldPosition ? loc.worldPosition.z : 'null'}
                )">
                    Save Changes
                </button>
            </div>`;
    }

    function submitUpdate(issueId, locationType, dbId, wx, wy, wz) {
        const title = document.getElementById('editTitle')?.value?.trim();
        const description = document.getElementById('editDescription')?.value?.trim();
        const type = parseInt(document.getElementById('editType')?.value || '0');

        if (!title) { alert('Title is required'); return; }
        if (!description) { alert('Description is required'); return; }

        updateIssue(issueId, {
            title,
            description,
            type,
            locationType,
            dbId,
            worldX: wx,
            worldY: wy,
            worldZ: wz
        });
    }

    function showListView() {
        selectedIssueId = null;
        const body = document.getElementById('panelBody');
        if (body) {
            body.innerHTML = `
                <div class="filter-bar">
                    <select id="filterStatus" class="form-select" onchange="IssueManager.loadIssues()">
                        <option value="">All Status</option>
                        <option value="0">Open</option>
                        <option value="1">In Progress</option>
                        <option value="2">Done</option>
                    </select>
                    <select id="filterType" class="form-select" onchange="IssueManager.loadIssues()">
                        <option value="">All Types</option>
                        <option value="0">Quality Defect</option>
                        <option value="1">Safety Deficiency</option>
                        <option value="2">Construction Defect</option>
                        <option value="3">Design Change</option>
                    </select>
                </div>
                <div id="issueList" class="issue-list"></div>`;
        }
        loadIssues();
        // Clear viewer selection
        if (viewer) viewer.clearSelection();
    }

    // ??? UTILITIES ???
    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function showToast(message) {
        // Simple transient toast
        const toast = document.createElement('div');
        toast.className = 'alert alert-info position-fixed bottom-0 start-50 translate-middle-x mb-3';
        toast.style.zIndex = 9999;
        toast.textContent = message;
        document.body.appendChild(toast);
        setTimeout(() => toast.remove(), 3000);
    }

    // ── PUBLIC API ──
    return {
        init,
        loadIssues,
        selectIssue,
        changeStatus,
        deleteIssue,
        updateIssue,
        removePhoto,
        uploadPhoto,
        submitCreate,
        submitUpdate,
        showEditForm,
        showListView,
        togglePlacementMode
    };
})();
