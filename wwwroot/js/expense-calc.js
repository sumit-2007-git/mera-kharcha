// Expense Split Calculation Engine & Mock Receipt Scanner
document.addEventListener('DOMContentLoaded', function () {
    const baseAmountInput = document.getElementById('BaseAmount');
    const taxInput = document.getElementById('TaxAmount');
    const tipInput = document.getElementById('TipAmount');
    const serviceInput = document.getElementById('ServiceCharge');
    const discountInput = document.getElementById('DiscountAmount');
    const finalTotalDisplay = document.getElementById('finalTotalDisplay');
    const splitTypeSelect = document.getElementById('splitTypeSelect');
    const splitRows = document.querySelectorAll('.participant-split-row');

    function calculateFinalTotal() {
        const base = parseFloat(baseAmountInput?.value) || 0;
        const tax = parseFloat(taxInput?.value) || 0;
        const tip = parseFloat(tipInput?.value) || 0;
        const service = parseFloat(serviceInput?.value) || 0;
        const discount = parseFloat(discountInput?.value) || 0;

        const total = Math.max(0, (base + tax + tip + service) - discount);
        if (finalTotalDisplay) {
            finalTotalDisplay.innerText = '₹' + total.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        }
        return total;
    }

    function updateSplitInputsVisibility() {
        const selectedType = splitTypeSelect ? splitTypeSelect.value : "1"; // 1: Equal, 2: Exact, 3: Percentage, 4: Shares

        document.querySelectorAll('.split-col-exact').forEach(el => el.classList.toggle('d-none', selectedType !== "2" && selectedType !== "ExactAmount"));
        document.querySelectorAll('.split-col-percentage').forEach(el => el.classList.toggle('d-none', selectedType !== "3" && selectedType !== "Percentage"));
        document.querySelectorAll('.split-col-shares').forEach(el => el.classList.toggle('d-none', selectedType !== "4" && selectedType !== "Shares"));

        recalculateShares();
    }

    function recalculateShares() {
        const totalAmount = calculateFinalTotal();
        const selectedType = splitTypeSelect ? splitTypeSelect.value : "1";

        let includedCheckboxes = document.querySelectorAll('.participant-checkbox:checked');
        let count = includedCheckboxes.length;

        if (count === 0) {
            updateStatusBadge("Please select at least one participant", false);
            return;
        }

        if (selectedType === "1" || selectedType === "Equal") {
            const equalShare = count > 0 ? (totalAmount / count).toFixed(2) : 0;
            splitRows.forEach(row => {
                const cb = row.querySelector('.participant-checkbox');
                const badge = row.querySelector('.calculated-share-badge');
                if (badge) {
                    if (cb && cb.checked) {
                        badge.innerText = '₹' + parseFloat(equalShare).toLocaleString('en-IN', { minimumFractionDigits: 2 });
                    } else {
                        badge.innerText = '₹0.00';
                    }
                }
            });
            updateStatusBadge(`Evenly split: ₹${equalShare} / person`, true);
        }
        else if (selectedType === "2" || selectedType === "ExactAmount") {
            let sumExact = 0;
            splitRows.forEach(row => {
                const cb = row.querySelector('.participant-checkbox');
                const exactInp = row.querySelector('.exact-amount-input');
                const badge = row.querySelector('.calculated-share-badge');
                if (cb && cb.checked && exactInp) {
                    const val = parseFloat(exactInp.value) || 0;
                    sumExact += val;
                    if (badge) badge.innerText = '₹' + val.toFixed(2);
                } else if (badge) {
                    badge.innerText = '₹0.00';
                }
            });

            const diff = (totalAmount - sumExact).toFixed(2);
            if (Math.abs(diff) < 0.05) {
                updateStatusBadge(`Exact amounts match total perfectly! (₹${sumExact.toFixed(2)})`, true);
            } else if (diff > 0) {
                updateStatusBadge(`Remaining to assign: ₹${diff}`, false);
            } else {
                updateStatusBadge(`Exceeds total by: ₹${Math.abs(diff)}`, false);
            }
        }
        else if (selectedType === "3" || selectedType === "Percentage") {
            let sumPct = 0;
            splitRows.forEach(row => {
                const cb = row.querySelector('.participant-checkbox');
                const pctInp = row.querySelector('.percentage-input');
                const badge = row.querySelector('.calculated-share-badge');
                if (cb && cb.checked && pctInp) {
                    const pct = parseFloat(pctInp.value) || 0;
                    sumPct += pct;
                    const share = (totalAmount * (pct / 100)).toFixed(2);
                    if (badge) badge.innerText = '₹' + share;
                } else if (badge) {
                    badge.innerText = '₹0.00';
                }
            });

            const diffPct = (100 - sumPct).toFixed(1);
            if (Math.abs(diffPct) < 0.1) {
                updateStatusBadge(`Percentages total 100%!`, true);
            } else {
                updateStatusBadge(`Total: ${sumPct.toFixed(1)}% (${diffPct > 0 ? diffPct + '% remaining' : Math.abs(diffPct) + '% over'})`, false);
            }
        }
        else if (selectedType === "4" || selectedType === "Shares") {
            let totalShares = 0;
            splitRows.forEach(row => {
                const cb = row.querySelector('.participant-checkbox');
                const shInp = row.querySelector('.shares-input');
                if (cb && cb.checked && shInp) {
                    totalShares += parseFloat(shInp.value) || 1;
                }
            });

            splitRows.forEach(row => {
                const cb = row.querySelector('.participant-checkbox');
                const shInp = row.querySelector('.shares-input');
                const badge = row.querySelector('.calculated-share-badge');
                if (cb && cb.checked && shInp && totalShares > 0) {
                    const sh = parseFloat(shInp.value) || 1;
                    const share = (totalAmount * (sh / totalShares)).toFixed(2);
                    if (badge) badge.innerText = '₹' + share;
                } else if (badge) {
                    badge.innerText = '₹0.00';
                }
            });

            updateStatusBadge(`Total: ${totalShares} shares calculated`, true);
        }
    }

    function updateStatusBadge(text, isValid) {
        const badge = document.getElementById('splitValidationBadge');
        if (!badge) return;
        badge.innerText = text;
        badge.className = isValid 
            ? 'badge bg-success-subtle text-success border border-success-subtle px-3 py-2'
            : 'badge bg-warning-subtle text-warning-emphasis border border-warning-subtle px-3 py-2';
    }

    // Attach listeners
    [baseAmountInput, taxInput, tipInput, serviceInput, discountInput].forEach(inp => {
        if (inp) inp.addEventListener('input', recalculateShares);
    });

    if (splitTypeSelect) {
        splitTypeSelect.addEventListener('change', updateSplitInputsVisibility);
    }

    document.querySelectorAll('.participant-checkbox, .exact-amount-input, .percentage-input, .shares-input').forEach(inp => {
        inp.addEventListener('input', recalculateShares);
    });

    // Select All / Deselect All
    const selectAllBtn = document.getElementById('selectAllParticipants');
    if (selectAllBtn) {
        selectAllBtn.addEventListener('click', function () {
            document.querySelectorAll('.participant-checkbox').forEach(cb => cb.checked = true);
            recalculateShares();
        });
    }

    // Initial Run
    updateSplitInputsVisibility();

    // ----------------- Multi-Currency & Forex Converter -----------------
    const currencySelect = document.getElementById('currencySelect');
    const currencySymbolBadge = document.getElementById('currencySymbolBadge');
    const exchangeRateInput = document.getElementById('exchangeRateInput');
    const originalAmountInput = document.getElementById('originalAmountInput');
    const forexBanner = document.getElementById('forexBanner');
    const forexRateText = document.getElementById('forexRateText');
    const forexOriginalText = document.getElementById('forexOriginalText');

    function updateCurrencyForex() {
        if (!currencySelect) return;
        const selectedOpt = currencySelect.options[currencySelect.selectedIndex];
        const code = currencySelect.value;
        const symbol = selectedOpt?.getAttribute('data-symbol') || '₹';
        const rate = parseFloat(selectedOpt?.getAttribute('data-rate')) || 1.0;

        if (exchangeRateInput) exchangeRateInput.value = rate;

        if (code === 'INR') {
            if (currencySymbolBadge) currencySymbolBadge.innerText = '₹';
            if (forexBanner) forexBanner.classList.add('d-none');
            if (originalAmountInput) originalAmountInput.value = baseAmountInput?.value || 0;
        } else {
            if (currencySymbolBadge) currencySymbolBadge.innerText = symbol;
            if (forexBanner) {
                forexBanner.classList.remove('d-none');
                if (forexRateText) forexRateText.innerText = `1 ${code} = ₹${rate.toFixed(2)} INR`;
            }
            // Recalculate original vs INR
            const currentBase = parseFloat(baseAmountInput?.value) || 0;
            if (originalAmountInput) {
                const foreignAmt = rate > 0 ? (currentBase / rate).toFixed(2) : currentBase;
                originalAmountInput.value = foreignAmt;
                if (forexOriginalText) forexOriginalText.innerText = `${symbol}${foreignAmt} ${code}`;
            }
        }
    }

    if (currencySelect) {
        currencySelect.addEventListener('change', updateCurrencyForex);
    }

    // ----------------- AI Bill Scanner & Itemized Bill Splitting -----------------
    const scanUploadedBtn = document.getElementById('scanUploadedBtn');
    const receiptFileInput = document.getElementById('ReceiptFile');
    const presetButtons = document.querySelectorAll('.preset-btn');
    const aiItemsContainer = document.getElementById('aiItemsContainer');
    const aiItemsTableBody = document.getElementById('aiItemsTableBody');
    const aiVendorName = document.getElementById('aiVendorName');
    const aiScanSummaryText = document.getElementById('aiScanSummaryText');
    const applyItemizedSplitBtn = document.getElementById('applyItemizedSplitBtn');

    let currentScannedBill = null;

    function handleReceiptScan(presetId = null) {
        const formData = new FormData();
        if (presetId) {
            formData.append('presetId', presetId);
        } else if (receiptFileInput && receiptFileInput.files && receiptFileInput.files.length > 0) {
            formData.append('file', receiptFileInput.files[0]);
        } else {
            alert('Please choose an image/receipt file first, or click one of the quick preset buttons!');
            if (receiptFileInput) receiptFileInput.click();
            return;
        }

        if (scanUploadedBtn) {
            scanUploadedBtn.disabled = true;
            scanUploadedBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> AI Extracting Items...';
        }

        fetch('/Expenses/ScanReceipt', {
            method: 'POST',
            body: formData
        })
        .then(res => res.json())
        .then(data => {
            if (scanUploadedBtn) {
                scanUploadedBtn.disabled = false;
                scanUploadedBtn.innerHTML = '<i class="bi bi-magic me-1"></i> Scan Uploaded File';
            }

            if (data.success) {
                currentScannedBill = data;
                renderScannedItems(data);

                // Auto-fill form values
                const titleInp = document.getElementById('Title');
                if (titleInp && !titleInp.value) titleInp.value = data.vendorName;

                if (baseAmountInput) baseAmountInput.value = data.subTotal;
                if (taxInput) taxInput.value = data.taxAmount;
                if (tipInput) tipInput.value = data.tipAmount;
                if (serviceInput) serviceInput.value = data.serviceCharge;
                if (discountInput) discountInput.value = data.discountAmount;

                const dateInp = document.getElementById('ExpenseDate');
                if (dateInp && data.billDate) {
                    dateInp.value = data.billDate.split('T')[0];
                }

                // Match Category
                const catSelect = document.getElementById('CategoryId');
                if (catSelect && data.categoryName) {
                    for (let opt of catSelect.options) {
                        if (opt.text.toLowerCase().includes(data.categoryName.toLowerCase())) {
                            catSelect.value = opt.value;
                            break;
                        }
                    }
                }

                calculateFinalTotal();
                recalculateShares();

                if (typeof showToast === 'function') {
                    showToast(`✨ AI Extracted ${data.items ? data.items.length : 0} items from "${data.vendorName}"!`, 'success');
                }
            }
        })
        .catch(err => {
            if (scanUploadedBtn) {
                scanUploadedBtn.disabled = false;
                scanUploadedBtn.innerHTML = '<i class="bi bi-magic me-1"></i> Scan Uploaded File';
            }
            alert('Scan error: ' + err);
        });
    }

    function renderScannedItems(bill) {
        if (!aiItemsContainer || !aiItemsTableBody) return;
        aiItemsContainer.classList.remove('d-none');
        if (aiVendorName) aiVendorName.innerText = bill.vendorName + ` (₹${bill.totalAmount.toFixed(2)})`;
        if (aiScanSummaryText) aiScanSummaryText.innerText = bill.rawSummary || 'Items extracted. Check the participants who consumed each item:';

        aiItemsTableBody.innerHTML = '';
        const members = window.groupMembers || [];

        bill.items.forEach((item, idx) => {
            const tr = document.createElement('tr');

            let memberCheckboxesHtml = '<div class="d-flex flex-wrap gap-2">';
            members.forEach(m => {
                const memberId = m.userId || m.UserId;
                const memberName = m.fullName || m.FullName || 'Member';
                memberCheckboxesHtml += `
                    <label class="form-check-label badge bg-body-tertiary text-body border px-2 py-1 user-select-none" style="cursor: pointer;">
                        <input type="checkbox" class="form-check-input me-1 item-member-cb" data-item-idx="${idx}" data-user-id="${memberId}" checked />
                        ${memberName.split(' ')[0]}
                    </label>
                `;
            });
            memberCheckboxesHtml += '</div>';

            tr.innerHTML = `
                <td class="fw-semibold">${item.name}</td>
                <td><span class="badge bg-secondary-subtle text-secondary">${item.quantity}x</span></td>
                <td class="fw-bold text-primary">₹${item.price.toFixed(2)}</td>
                <td>${memberCheckboxesHtml}</td>
            `;
            aiItemsTableBody.appendChild(tr);
        });
    }

    if (scanUploadedBtn) {
        scanUploadedBtn.addEventListener('click', () => handleReceiptScan(null));
    }

    presetButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            const presetId = this.getAttribute('data-preset');
            handleReceiptScan(presetId);
        });
    });

    if (applyItemizedSplitBtn) {
        applyItemizedSplitBtn.addEventListener('click', function () {
            if (!currentScannedBill || !currentScannedBill.items) return;

            const members = window.groupMembers || [];
            const userShares = {};
            members.forEach(m => {
                const uId = m.userId || m.UserId;
                userShares[uId] = 0;
            });

            // Calculate consumption per item
            currentScannedBill.items.forEach((item, idx) => {
                const checkedCbs = aiItemsTableBody.querySelectorAll(`.item-member-cb[data-item-idx="${idx}"]:checked`);
                const consumersCount = checkedCbs.length;
                if (consumersCount > 0) {
                    const itemTotal = item.price * (item.quantity || 1);
                    const splitPerPerson = itemTotal / consumersCount;
                    checkedCbs.forEach(cb => {
                        const uId = cb.getAttribute('data-user-id');
                        if (userShares[uId] !== undefined) {
                            userShares[uId] += splitPerPerson;
                        }
                    });
                }
            });

            // Add proportional share of extra taxes, tips, discounts
            const subTotal = currentScannedBill.subTotal || 1;
            const extraAdjustments = (currentScannedBill.taxAmount || 0) + 
                                     (currentScannedBill.tipAmount || 0) + 
                                     (currentScannedBill.serviceCharge || 0) - 
                                     (currentScannedBill.discountAmount || 0);

            // Switch Split Type to "Exact Amounts"
            if (splitTypeSelect) {
                splitTypeSelect.value = "2"; // ExactAmount
                updateSplitInputsVisibility();
            }

            // Populate exact amount inputs
            splitRows.forEach(row => {
                const cb = row.querySelector('.participant-checkbox');
                const exactInp = row.querySelector('.exact-amount-input');
                const userIdInp = row.querySelector('input[type="hidden"][name$=".UserId"]');

                if (userIdInp) {
                    const uId = userIdInp.value;
                    const itemSubtotal = userShares[uId] || 0;
                    if (itemSubtotal > 0) {
                        const ratio = subTotal > 0 ? (itemSubtotal / subTotal) : 0;
                        const netShare = Math.max(0, itemSubtotal + (extraAdjustments * ratio));

                        if (cb) cb.checked = true;
                        if (exactInp) exactInp.value = netShare.toFixed(2);
                    } else {
                        if (cb) cb.checked = false;
                        if (exactInp) exactInp.value = "0.00";
                    }
                }
            });

            recalculateShares();

            if (typeof showToast === 'function') {
                showToast('✅ Itemized bill successfully divided and applied to exact split table!', 'success');
            }
        });
    }
});
